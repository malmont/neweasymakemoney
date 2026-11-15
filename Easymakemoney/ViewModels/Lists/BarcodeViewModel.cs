using Easymakemoney.Services.Api;
using Easymakemoney.Services.Dialog;

namespace Easymakemoney.ViewModels.Lists
{
    /// <summary>
    /// Écran « Codes-barres » (backend CB-3) : lire ou taper un code et voir ce qui le porte, poser un code libre (celui du fabricant)
    /// sur une fiche, générer le code d'une fiche ou tous les codes manquants, remplacer ou retirer un code, imprimer des étiquettes.
    /// Le serveur garantit qu'un code désigne une seule chose ; changer ou retirer un code déjà posé demande toujours une confirmation.
    /// </summary>
    public partial class BarcodeViewModel : BaseViewModel
    {
        /// <summary>Garde-fou : un lancement écrit au plus 500 codes ; on n'enchaîne pas plus de lancements que cela.</summary>
        private const int MaxGenerateRuns = 40;

        private readonly ICatalogBarcodeService _service;
        private readonly IProductAttachmentService _products;
        private readonly IListProductVarianstService _variants;
        private readonly IBarcodeScanner _scanner;
        private readonly IDialogService _dialogService;

        public BarcodeViewModel(ICatalogBarcodeService service, IProductAttachmentService products, IListProductVarianstService variants,
            IBarcodeScanner scanner, IDialogService dialogService)
        {
            _service = service;
            _products = products;
            _variants = variants;
            _scanner = scanner;
            _dialogService = dialogService;
        }

        [ObservableProperty]
        private string? _code;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Sheet))]
        [NotifyPropertyChangedFor(nameof(HasSheet))]
        [NotifyPropertyChangedFor(nameof(CanAssign))]
        private BarcodeSearch? _result;

        [ObservableProperty]
        private BarcodeMissing? _missing;

        public BarcodeSheet? Sheet => Result?.IsFound == true ? Result.sheet : null;
        public bool HasSheet => Sheet != null;
        public bool CanAssign => Result?.CanAssign == true;

        /// <summary>Ouverture du PDF des étiquettes (partage vers l'imprimante) ; les tests la remplacent.</summary>
        public Func<DownloadedFile, Task> OpenFile { get; set; } = OpenWithShareAsync;

        public Task LoadAsync() => RunAsync(RefreshMissingAsync);

        private async Task RefreshMissingAsync() => Missing = await _service.GetMissingAsync();

        [RelayCommand]
        private Task Scan() => RunAsync(async () =>
        {
            var value = await _scanner.ScanAsync();
            if (string.IsNullOrWhiteSpace(value))
                return;
            Code = value;
            await SearchCoreAsync();
        });

        [RelayCommand]
        private Task Search() => RunAsync(SearchCoreAsync);

        private async Task SearchCoreAsync()
        {
            // Jamais de recherche vide : le serveur la refuserait.
            if (string.IsNullOrWhiteSpace(Code))
                return;
            Result = await _service.SearchAsync(Code);
        }

        /// <summary>Poser le code lu, libre, sur une fiche choisie par son nom.</summary>
        [RelayCommand]
        private Task Assign() => RunAsync(async () =>
        {
            if (Result is not { CanAssign: true, code: { } code })
                return;
            var target = await PickSheetAsync($"Poser le code {code}");
            if (target == null)
                return;
            await WriteAsync(target, code);
        });

        /// <summary>Donner un code généré à une fiche qui n'en a pas.</summary>
        [RelayCommand]
        private Task GenerateForSheet() => RunAsync(async () =>
        {
            var target = await PickSheetAsync("Générer un code-barres");
            if (target == null)
                return;
            try
            {
                var generated = await _service.GenerateAsync(target.Variant, target.Id);
                ShowSheet(generated.barcode, generated.sheet);
                await RefreshMissingAsync();
            }
            catch (ApiException ex) when (ex.ApiError.Code == "BARCODE_ALREADY_SET")
            {
                await _dialogService.ShowInfoAsync($"{target.Label} a déjà un code-barres : rien n'est remplacé.");
            }
        });

        /// <summary>Remplacer le code de la fiche affichée par un code lu ou tapé.</summary>
        [RelayCommand]
        private Task Replace() => RunAsync(async () =>
        {
            if (Sheet is not { } sheet)
                return;
            var newCode = await AskCodeAsync("Nouveau code-barres");
            if (string.IsNullOrWhiteSpace(newCode))
                return;
            var target = new PickedSheet(sheet.IsVariant, sheet.SheetId, sheet.Title);
            await WriteAsync(target, newCode.Trim());
        });

        [RelayCommand]
        private Task Remove() => RunAsync(async () =>
        {
            if (Sheet is not { HasBarcode: true } sheet)
                return;
            var ok = await _dialogService.ConfirmAsync("Retirer le code-barres",
                $"Retirer le code {sheet.barcode} de {sheet.Title} ? La caisse ne trouvera plus cette fiche par ce code.", "Retirer", "Annuler");
            if (!ok)
                return;
            await _service.SetAsync(sheet.IsVariant, sheet.SheetId, null, replace: true);
            Result = null;
            await _dialogService.ShowInfoAsync($"Code retiré de {sheet.Title}.");
            await RefreshMissingAsync();
        });

        [RelayCommand]
        private Task PrintLabel() => RunAsync(async () =>
        {
            if (Sheet is not { HasBarcode: true } sheet)
                return;
            var copiesText = await _dialogService.PromptAsync("Étiquettes", $"Combien d'étiquettes pour {sheet.Title} ?", "Imprimer", "Annuler",
                initialValue: "1", keyboard: Keyboard.Numeric);
            if (copiesText == null)
                return;
            if (!int.TryParse(copiesText.Trim(), out var copies) || copies < 1 || copies > 500)
                throw new UserMessageException("Indiquez un nombre d'étiquettes entre 1 et 500.");
            var showPrice = sheet.priceCents.HasValue
                && await _dialogService.ConfirmAsync("Étiquettes", "Imprimer le prix sur l'étiquette ?", "Avec le prix", "Sans le prix");
            var file = await _service.LabelsPdfAsync(new[] { new BarcodeLabelRequest(sheet, copies) }, showPrice);
            await OpenFile(file);
        });

        [RelayCommand]
        private Task GenerateMissing() => RunAsync(async () =>
        {
            await RefreshMissingAsync();
            if (Missing is not { HasMissing: true } missing)
            {
                await _dialogService.ShowInfoAsync("Toutes les fiches ont déjà un code-barres.");
                return;
            }
            var ok = await _dialogService.ConfirmAsync("Générer les codes manquants",
                $"Donner un code-barres aux {missing.total} fiche(s) qui n'en ont pas ? Aucun code existant n'est remplacé.", "Générer", "Annuler");
            if (!ok)
                return;

            var written = 0;
            var remaining = 0;
            for (var run = 0; run < MaxGenerateRuns; run++)
            {
                var result = await _service.GenerateMissingAsync();
                written += result.written;
                remaining = result.remaining;
                // Plus rien à faire, ou rien d'écrit (fiches occupées par une vente) : on s'arrête, sans boucler.
                if (result.remaining <= 0 || result.written <= 0)
                    break;
            }

            var text = written == 1 ? "1 code-barres généré." : $"{written} codes-barres générés.";
            if (remaining > 0)
                text += $" {remaining} fiche(s) occupée(s) par une vente en cours : relancez dans un instant.";
            await _dialogService.ShowInfoAsync(text);
            await RefreshMissingAsync();
        });

        /// <summary>Pose d'un code ; si la fiche en a déjà un autre, demande avant de le remplacer.</summary>
        private async Task WriteAsync(PickedSheet target, string code)
        {
            BarcodeWrite write;
            try
            {
                write = await _service.SetAsync(target.Variant, target.Id, code, replace: false);
            }
            catch (ApiException ex) when (ex.ApiError.Code == "BARCODE_ALREADY_SET")
            {
                var current = ex.ApiError.Body?["current"]?.ToString();
                var question = string.IsNullOrWhiteSpace(current)
                    ? $"{target.Label} a déjà un code-barres. Le remplacer par {code} ?"
                    : $"{target.Label} a déjà le code {current}. Le remplacer par {code} ?";
                if (!await _dialogService.ConfirmAsync("Remplacer le code-barres", question, "Remplacer", "Annuler"))
                    return;
                write = await _service.SetAsync(target.Variant, target.Id, code, replace: true);
            }

            ShowSheet(write.barcode, write.sheet);
            if (!string.IsNullOrWhiteSpace(write.warning?.message))
                await _dialogService.ShowInfoAsync(write.warning!.message!, "Code enregistré");
            await RefreshMissingAsync();
        }

        private void ShowSheet(string? barcode, BarcodeSheet? sheet)
        {
            if (sheet == null)
                return;
            Code = barcode ?? sheet.barcode;
            Result = new BarcodeSearch { code = Code, found = true, sheet = sheet };
        }

        private async Task<string?> AskCodeAsync(string title)
        {
            var how = await _dialogService.ChooseAsync(title, new[] { "Scanner", "Taper le code" });
            return how switch
            {
                "Scanner" => await _scanner.ScanAsync(),
                "Taper le code" => await _dialogService.PromptAsync(title, "Code-barres", "Valider", "Annuler"),
                _ => null,
            };
        }

        /// <summary>Choix d'une fiche : un article par son nom, puis sa variante s'il en a.</summary>
        private async Task<PickedSheet?> PickSheetAsync(string title)
        {
            var query = await _dialogService.PromptAsync(title, "Nom de l'article", "Chercher", "Annuler");
            if (string.IsNullOrWhiteSpace(query))
                return null;
            var page = await _products.GetAvailableAsync(query.Trim(), 1, 20, allProducts: true);
            if (page.items.Count == 0)
            {
                await _dialogService.ShowInfoAsync($"Aucun article ne correspond à « {query.Trim()} ».");
                return null;
            }

            var productLabels = UniqueLabels(page.items.Select(p => string.IsNullOrWhiteSpace(p.code) ? p.name : $"{p.name} · {p.code}"));
            var chosenProduct = await _dialogService.ChooseAsync(title, productLabels);
            var productIndex = chosenProduct == null ? -1 : productLabels.IndexOf(chosenProduct);
            if (productIndex < 0)
                return null;
            var product = page.items[productIndex];

            var variants = (await _variants.GetProductVariantList(product.id)).ToList();
            if (variants.Count == 0)
                return new PickedSheet(false, product.id, product.name);

            var variantLabels = UniqueLabels(variants.Select(VariantLabel));
            var chosenVariant = await _dialogService.ChooseAsync(product.name, variantLabels);
            var variantIndex = chosenVariant == null ? -1 : variantLabels.IndexOf(chosenVariant);
            if (variantIndex < 0)
                return null;
            return new PickedSheet(true, variants[variantIndex].id, $"{product.name} · {VariantLabel(variants[variantIndex])}");
        }

        private static string VariantLabel(ProductVariant v)
        {
            var parts = new[] { v.color?.name, v.size?.name }.Where(s => !string.IsNullOrWhiteSpace(s));
            var label = string.Join(" / ", parts);
            return string.IsNullOrWhiteSpace(label) ? $"Variante {v.id}" : label;
        }

        /// <summary>Le choix se fait par le texte : deux libellés identiques reçoivent un numéro pour rester distincts.</summary>
        private static List<string> UniqueLabels(IEnumerable<string> labels)
        {
            var result = new List<string>();
            foreach (var label in labels)
            {
                var text = label;
                var n = 2;
                while (result.Contains(text))
                    text = $"{label} ({n++})";
                result.Add(text);
            }
            return result;
        }

        private async Task RunAsync(Func<Task> action)
        {
            if (IsBusy)
                return;
            IsBusy = true;
            try
            {
                await action();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                await _dialogService.ShowErrorAsync(ex);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private static async Task OpenWithShareAsync(DownloadedFile file)
        {
            var path = Path.Combine(FileSystem.CacheDirectory, $"etiquettes-{DateTime.Now:yyyyMMdd-HHmmss}.pdf");
            await File.WriteAllBytesAsync(path, file.Content);
            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = "Étiquettes",
                File = new ShareFile(path, "application/pdf"),
            });
        }

        private sealed record PickedSheet(bool Variant, int Id, string Label);
    }
}
