using System.Globalization;
using System.Net;
using Easymakemoney.Services.Api;
using Easymakemoney.Services.Dialog;
using Easymakemoney.UseCase.DocumentUseCase;
using Easymakemoney.Utilities;
using Newtonsoft.Json.Linq;

namespace Easymakemoney.ViewModels.Lists
{
    /// <summary>Un fournisseur dans le sélecteur : proposé par la lecture, ou pris dans la liste.</summary>
    public sealed class InvoiceSupplierChoice
    {
        public required ListFournisseur Supplier { get; init; }
        public required string Label { get; init; }
    }

    /// <summary>
    /// Vérification d'une facture fournisseur lue par l'IA : le gérant contrôle chaque ligne, choisit le fournisseur,
    /// puis valide. Rien n'est créé sans son geste ; son travail est gardé en brouillon au fil de l'eau.
    /// </summary>
    public partial class InvoiceReviewViewModel : BaseViewModel, IInvoiceLineHost
    {
        private const int DraftDelayMs = 1000;
        private const string TakePhotoChoice = "Prendre une photo";
        private const string PickPhotoChoice = "Choisir une photo";

        private static readonly Microsoft.Maui.Graphics.Color WarningColor = Microsoft.Maui.Graphics.Color.FromArgb("#B45309");
        private static readonly Microsoft.Maui.Graphics.Color NeutralColor = Microsoft.Maui.Graphics.Color.FromArgb("#0F172A");

        private readonly DocumentReaderUseCase _documents;
        private readonly IListFourniseursServices _suppliers;
        private readonly ICommandeReceptionService _reception;
        private readonly IDialogService _dialogService;
        private readonly SemaphoreSlim _draftLock = new(1, 1);

        private int _documentId;
        private InvoiceExtraction _extraction = new();
        private bool _loading;
        private CancellationTokenSource? _draftCts;
        private bool _draftPending;
        private string? _lastDraftJson;

        /// <summary>Brouillon tel qu'il est arrivé du serveur, pour garder à la réécriture les champs que cette version ne connaît pas.</summary>
        private Newtonsoft.Json.Linq.JToken? _savedDraft;

        /// <summary>Choix dans une liste (variante reçue) ; remplaçable dans les tests.</summary>
        public Func<string, string[], Task<string?>> ChooseAsync { get; set; }

        /// <summary>Faux tant que la route d'aperçu répond 404 (pas encore en ligne) : l'estimation locale reste alors affichée.</summary>
        private bool _previewAvailable = true;

        /// <summary>
        /// Lot APERÇU-FACT : prix de vente prévu, marge et total des lignes corrigées, calculés par le serveur. Sans réseau, sans route
        /// (404) ou si la facture est incomplète, rien n'est demandé.
        /// </summary>
        public async Task RefreshPreviewAsync()
        {
            if (!_previewAvailable || _documentId == 0 || !IsLoaded)
                return;

            // L'aperçu sert pendant la vérification : les lignes comptent comme validées pour le calcul, rien n'est enregistré.
            var draft = BuildDraft();
            draft.Lines = draft.Lines.Select(line => { var copy = line.Clone(); copy.Validated = true; return copy; }).ToList();
            var result = InvoiceConfirmationBuilder.Build(draft, Context());
            if (!result.IsValid)
            {
                foreach (var line in Lines)
                    line.ServerPriceText = line.IsExisting ? null : "Prix de vente : calculé quand la facture est complète";
                LinesTotalText = "À compléter";
                return;
            }

            try
            {
                var preview = await _documents.PreviewAsync(_documentId, result.Body!);
                foreach (var line in Lines)
                    line.ServerPriceText = preview.Line(line.N) is { } server ? preview.PriceText(server) : null;
                if (preview.LinesTotalText is { } total)
                    LinesTotalText = total;
            }
            catch (ApiException e) when (e.StatusCode == HttpStatusCode.NotFound && e.ApiError.Code is not ("DOCUMENT_NOT_FOUND"))
            {
                // Route pas encore en ligne : on garde l'estimation locale, sans redemander à chaque modification.
                _previewAvailable = false;
                foreach (var line in Lines)
                    line.ServerPriceText = null;
                RefreshState();
            }
            catch (Exception e)
            {
                Debug.WriteLine($"[InvoiceReview] Aperçu indisponible : {e.Message}");
            }
        }

        private Newtonsoft.Json.Linq.JObject DraftJson() => InvoiceReviewDraft.KeepUnknownFields(BuildDraft().ToJson(), _savedDraft);

        public InvoiceReviewViewModel(DocumentReaderUseCase documents, IListFourniseursServices suppliers,
            ICommandeReceptionService reception, IDialogService dialogService)
        {
            _documents = documents;
            _suppliers = suppliers;
            _reception = reception;
            _dialogService = dialogService;
            ChooseAsync = (title, labels) => _dialogService.ChooseAsync(title, labels, "Plus tard");
        }

        // ----- état de l'écran -----

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasLoadError))]
        private string? loadError;

        public bool HasLoadError => !string.IsNullOrEmpty(LoadError);

        [ObservableProperty] private bool isLoaded;
        [ObservableProperty] private bool isLoading;
        [ObservableProperty] private string? remarksText;
        [ObservableProperty] private bool hasRemarks;
        [ObservableProperty] private string? draftStatusText;

        // ----- fournisseur -----

        [ObservableProperty] private string? supplierReadText;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsExistingSupplier))]
        private bool isNewSupplier;

        public bool IsExistingSupplier => !IsNewSupplier;

        public ObservableCollection<InvoiceSupplierChoice> SupplierChoices { get; } = new();
        public ObservableCollection<TypeFournisseur> SupplierTypes { get; } = new();

        [ObservableProperty] private InvoiceSupplierChoice? selectedSupplier;
        [ObservableProperty] private string? newSupplierName;
        [ObservableProperty] private TypeFournisseur? selectedSupplierType;
        [ObservableProperty] private string? supplierWarning;
        [ObservableProperty] private bool hasSupplierWarning;

        // ----- facture -----

        [ObservableProperty] private string? invoiceNumber;
        [ObservableProperty] private DateTime invoiceDate = DateTime.Today;
        [ObservableProperty] private string? exchangeRateText;
        [ObservableProperty] private bool needsExchangeRate;
        [ObservableProperty] private string? exchangeRateLabel;
        [ObservableProperty] private string? currencyText;

        // ----- totaux -----

        [ObservableProperty] private bool hasTotals;
        [ObservableProperty] private string? readTotalText;
        [ObservableProperty] private string? computedTotalText;
        [ObservableProperty] private string? differenceText;
        [ObservableProperty] private Microsoft.Maui.Graphics.Color differenceColor = NeutralColor;
        [ObservableProperty] private string? linesTotalText;

        // ----- frais de port -----

        [ObservableProperty] private bool hasShipping;
        [ObservableProperty] private string? shippingLabel;
        [ObservableProperty] private bool includeShipping;
        [ObservableProperty] private string? shippingAmountText;

        // ----- lignes -----

        public ObservableCollection<InvoiceLineItemViewModel> Lines { get; } = new();

        [ObservableProperty] private string? validatedText;
        [ObservableProperty] private bool hasLinesToValidate;
        [ObservableProperty] private string? confirmHint;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
        private bool canConfirm;

        // ----- ouverture -----

        public async Task InitAsync(int documentId)
        {
            _documentId = documentId;
            await LoadAsync();
        }

        [RelayCommand]
        private async Task Reload() => await LoadAsync();

        private async Task LoadAsync()
        {
            if (IsLoading || _documentId == 0)
                return;

            IsLoading = true;
            LoadError = null;
            GestionDocument document;
            try
            {
                var documentTask = _documents.GetAsync(_documentId);
                var suppliersTask = _suppliers.GetFourniseursList();
                var typesTask = _suppliers.GetTypeFournisseurs();
                await Task.WhenAll(documentTask, suppliersTask, typesTask);
                document = documentTask.Result;

                SupplierTypes.Clear();
                foreach (var type in typesTask.Result)
                    SupplierTypes.Add(type);
                BuildSupplierChoices(suppliersTask.Result.ToList(), InvoiceExtraction.Read(document?.extraction));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                LoadError = "Impossible d'ouvrir la facture. Vérifiez la connexion et réessayez.";
                IsLoading = false;
                return;
            }

            IsLoading = false;
            if (document == null)
            {
                LoadError = "Facture introuvable.";
                return;
            }

            switch (document.status)
            {
                case "extracted":
                    break;
                case "reading":
                    LoadError = "La lecture de la facture est encore en cours. Réessayez dans un instant.";
                    return;
                case "confirmed":
                    LoadError = "Cette facture est déjà validée.";
                    return;
                case "expired":
                    LoadError = "Cette facture n'a pas été validée dans les 30 jours : elle a été effacée. Lisez-la à nouveau.";
                    return;
                default:
                    LoadError = "La lecture de cette facture a échoué. Lisez-la à nouveau.";
                    return;
            }

            ApplyDocument(document);
            IsLoaded = true;
            await RefreshPreviewAsync();
        }

        private void BuildSupplierChoices(List<ListFournisseur> suppliers, InvoiceExtraction extraction)
        {
            SupplierChoices.Clear();
            var used = new HashSet<int>();

            void Add(ListFournisseur supplier, string label)
            {
                if (used.Add(supplier.id))
                    SupplierChoices.Add(new InvoiceSupplierChoice { Supplier = supplier, Label = label });
            }

            if (extraction.SupplierId is > 0)
            {
                var recognized = suppliers.FirstOrDefault(s => s.id == extraction.SupplierId)
                    ?? new ListFournisseur { id = extraction.SupplierId.Value, name = extraction.SupplierName ?? $"Fournisseur {extraction.SupplierId}" };
                Add(recognized, $"{recognized.name} (reconnu)");
            }

            foreach (var candidate in extraction.SupplierCandidates.OrderByDescending(c => c.Score ?? 0))
            {
                var supplier = suppliers.FirstOrDefault(s => s.id == candidate.Id)
                    ?? new ListFournisseur { id = candidate.Id, name = candidate.Name ?? $"Fournisseur {candidate.Id}" };
                Add(supplier, $"{supplier.name} (proposé)");
            }

            foreach (var supplier in suppliers.OrderBy(s => s.name, StringComparer.CurrentCultureIgnoreCase))
                Add(supplier, supplier.name);
        }

        private void ApplyDocument(GestionDocument document)
        {
            _loading = true;
            try
            {
                _extraction = InvoiceExtraction.Read(document.extraction);
                var initial = InvoiceReview.InitialDraft(_extraction);
                var saved = InvoiceReviewDraft.FromJson(document.draft);
                _savedDraft = document.draft;
                var draft = InvoiceReview.Merge(initial, saved);

                ApplyRemarks();

                // Fournisseur
                SupplierReadText = string.IsNullOrWhiteSpace(_extraction.SupplierName)
                    ? "Nom du fournisseur non lu sur la facture."
                    : $"Lu sur la facture : {_extraction.SupplierName}";
                IsNewSupplier = draft.Supplier.Mode == InvoiceReviewDraft.ModeNew;
                SelectedSupplier = draft.Supplier.Mode == InvoiceReviewDraft.ModeExisting && draft.Supplier.Id is > 0
                    ? SupplierChoices.FirstOrDefault(c => c.Supplier.id == draft.Supplier.Id)
                    : null;
                NewSupplierName = IsNewSupplier ? draft.Supplier.Name : _extraction.SupplierName;
                SelectedSupplierType = SupplierTypes.FirstOrDefault(t => t.id == draft.Supplier.TypeFournisseurId);

                // Facture
                InvoiceNumber = draft.Invoice.Number;
                InvoiceDate = DateTime.TryParseExact(draft.Invoice.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
                    ? date
                    : DateTime.Today;
                ExchangeRateText = draft.Invoice.ExchangeRate;

                // Totaux lus et recalculés
                var currency = _extraction.DocumentCurrency;
                HasTotals = _extraction.TotalRead.HasValue || _extraction.ComputedTotal.HasValue;
                ReadTotalText = Format(_extraction.TotalRead, currency);
                ComputedTotalText = Format(_extraction.ComputedTotal, currency);
                var difference = _extraction.Difference ?? 0;
                DifferenceText = Format(difference, currency);
                DifferenceColor = difference != 0 ? WarningColor : NeutralColor;

                // Frais de port
                HasShipping = _extraction.ShippingAmount is > 0 || draft.Shipping.Include;
                ShippingLabel = string.IsNullOrWhiteSpace(_extraction.ShippingLabel)
                    ? "Frais de port lus sur la facture"
                    : $"Lu sur la facture : {_extraction.ShippingLabel}";
                IncludeShipping = draft.Shipping.Include;
                ShippingAmountText = draft.Shipping.Amount;

                // Lignes : les douteuses d'abord
                var photos = PhotoLines(document.linePhotos);
                var states = draft.Lines.ToDictionary(l => l.N);
                Lines.Clear();
                foreach (var read in InvoiceReview.OrderForReview(_extraction.Lines))
                {
                    var state = states.TryGetValue(read.N, out var kept) ? kept : InvoiceReview.InitialLine(read);
                    var item = new InvoiceLineItemViewModel(this, read, state);
                    if (photos.Contains(read.N))
                        item.SetPhoto(null);
                    Lines.Add(item);
                }

                // Un brouillon repris n'est pas renvoyé tant que rien ne change.
                _lastDraftJson = saved != null ? DraftJson().ToString(Formatting.None) : null;
                DraftStatusText = saved != null ? "Vérification reprise là où vous l'aviez laissée." : null;
            }
            finally
            {
                _loading = false;
            }

            RefreshState();
        }

        private void ApplyRemarks()
        {
            var remarks = new List<string>();
            if (_extraction.IsDuplicate)
            {
                remarks.Add(_extraction.DuplicateStatus == "confirmed"
                    ? "Cette facture semble déjà enregistrée (même fournisseur, même numéro). Vérifiez avant de la valider."
                    : "Une facture de ce fournisseur avec ce numéro est déjà en cours de vérification.");
            }
            if (_extraction.ConfidenceOverall is < InvoiceReview.LowConfidence || _extraction.ConfidenceLines is < InvoiceReview.LowConfidence)
                remarks.Add("Lecture incertaine : vérifiez chaque ligne.");
            remarks.AddRange(_extraction.Notes);
            RemarksText = string.Join("\n", remarks);
            HasRemarks = remarks.Count > 0;
        }

        private static HashSet<int> PhotoLines(JToken? linePhotos)
        {
            var lines = new HashSet<int>();
            if (linePhotos is JObject byNumber)
            {
                foreach (var property in byNumber.Properties())
                {
                    if (int.TryParse(property.Name, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n))
                        lines.Add(n);
                }
            }
            else if (linePhotos is JArray list)
            {
                foreach (var item in list.OfType<JObject>())
                {
                    if (item["n"]?.Type == JTokenType.Integer)
                        lines.Add(item["n"]!.Value<int>());
                }
            }
            return lines;
        }

        /// <summary>Estimation locale du total des lignes, utilisée seulement tant que l'aperçu du serveur n'est pas en ligne.</summary>
        private static string LocalLinesTotalText(InvoiceReviewDraft draft, string? currency)
        {
            long total = 0;
            foreach (var line in draft.Lines)
            {
                var quantity = InvoiceReview.ParseQuantity(line.Quantity);
                var cost = InvoiceReview.ParseCents(line.UnitCost);
                if (quantity == null || cost == null)
                    return "À compléter";
                total += quantity.Value * cost.Value;
            }
            return Format(total, currency);
        }

        private static string Format(long? cents, string? currency) =>
            cents.HasValue ? SiteCurrency.FormatCents(cents.Value, CultureInfo.CurrentCulture, currency) : "—";

        // ----- état calculé -----

        private string? SiteCurrencyCode => SiteCurrency.CurrentCurrency ?? _extraction.SiteCurrency;

        private InvoiceConfirmationContext Context(string? classificationDate = null, bool force = false) => new()
        {
            SiteCurrency = SiteCurrencyCode,
            DocumentCurrency = _extraction.DocumentCurrency,
            SupplierCurrency = IsNewSupplier ? null : SelectedSupplier?.Supplier.purchaseCurrency,
            ClassificationDate = classificationDate,
            Force = force,
        };

        private InvoiceReviewDraft BuildDraft() => new()
        {
            Supplier = new InvoiceReviewSupplier
            {
                Mode = IsNewSupplier ? InvoiceReviewDraft.ModeNew : InvoiceReviewDraft.ModeExisting,
                Id = IsNewSupplier ? null : SelectedSupplier?.Supplier.id,
                Name = IsNewSupplier ? NewSupplierName?.Trim() : SelectedSupplier?.Supplier.name,
                TypeFournisseurId = IsNewSupplier ? SelectedSupplierType?.id : null,
            },
            Invoice = new InvoiceReviewInvoice
            {
                Number = InvoiceNumber?.Trim(),
                Date = InvoiceDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                ExchangeRate = ExchangeRateText,
            },
            Shipping = new InvoiceReviewShipping { Include = IncludeShipping, Amount = ShippingAmountText },
            Lines = Lines.Select(l => l.ToLine()).OrderBy(l => l.N).ToList(),
        };

        private bool SupplierChosen => IsNewSupplier
            ? !string.IsNullOrWhiteSpace(NewSupplierName) && SelectedSupplierType != null
            : SelectedSupplier != null;

        private void RefreshState()
        {
            if (_loading)
                return;

            var draft = BuildDraft();
            var context = Context();

            // Devise et taux
            var site = SiteCurrencyCode;
            var document = _extraction.DocumentCurrency;
            var foreign = InvoiceConfirmationBuilder.ForeignCurrency(draft, context);
            NeedsExchangeRate = foreign != null;
            ExchangeRateLabel = foreign != null ? $"Taux de change : 1 {foreign} en {site ?? "devise du site"}" : null;
            CurrencyText = string.IsNullOrWhiteSpace(document) ? null : $"Montants de la facture en {document}, hors taxes.";
            var rate = foreign != null ? InvoiceReview.ParsePositive(ExchangeRateText) : null;
            foreach (var line in Lines)
                line.SetCurrency(document, site, rate);

            var supplierCurrency = IsNewSupplier ? null : SelectedSupplier?.Supplier.purchaseCurrency;
            SupplierWarning = !IsNewSupplier && SelectedSupplier != null
                && !InvoiceReview.SameCurrency(supplierCurrency ?? site, document)
                ? $"Ce fournisseur achète en {supplierCurrency ?? site}, la facture est en {document} : vérifiez le fournisseur."
                : null;
            HasSupplierWarning = SupplierWarning != null;

            // Regroupement des nouveaux articles par groupKey
            var groups = Lines.Where(l => l.IsNew && !string.IsNullOrWhiteSpace(l.GroupKey))
                .GroupBy(l => l.GroupKey!)
                .ToDictionary(g => g.Key, g => g.Count());
            foreach (var line in Lines)
            {
                var count = line.IsNew && line.GroupKey != null && groups.TryGetValue(line.GroupKey, out var c) ? c : 0;
                line.GroupText = count > 1
                    ? $"Même article que {count - 1} autre{(count > 2 ? "s" : string.Empty)} ligne{(count > 2 ? "s" : string.Empty)} : une variante par taille et couleur."
                    : null;
            }

            // Total de vos lignes : celui du serveur (aperçu) ; l'estimation locale seulement tant que la route n'existe pas.
            if (!_previewAvailable)
                LinesTotalText = LocalLinesTotalText(draft, document);

            // Compteur et validation
            var validated = Lines.Count(l => l.IsValidated);
            ValidatedText = $"{validated} sur {Lines.Count} validée{(validated > 1 ? "s" : string.Empty)}";
            HasLinesToValidate = validated < Lines.Count;
            var allValidated = Lines.Count > 0 && validated == Lines.Count;
            CanConfirm = allValidated && SupplierChosen;
            ConfirmHint = CanConfirm
                ? null
                : !allValidated
                    ? "Validez toutes les lignes pour continuer."
                    : "Choisissez le fournisseur pour continuer.";
        }

        // ----- modifications du formulaire -----

        private void FormChanged()
        {
            if (_loading)
                return;
            RefreshState();
            ScheduleDraftSave();
        }

        partial void OnIsNewSupplierChanged(bool value) => FormChanged();
        // Un sélecteur qui se vide en se recréant ne retire pas le choix du gérant.
        partial void OnSelectedSupplierChanged(InvoiceSupplierChoice? oldValue, InvoiceSupplierChoice? newValue)
        {
            if (newValue == null && oldValue != null && !_loading)
            {
                SelectedSupplier = oldValue;
                return;
            }
            FormChanged();
        }

        partial void OnNewSupplierNameChanged(string? value) => FormChanged();

        partial void OnSelectedSupplierTypeChanged(TypeFournisseur? oldValue, TypeFournisseur? newValue)
        {
            if (newValue == null && oldValue != null && !_loading)
            {
                SelectedSupplierType = oldValue;
                return;
            }
            FormChanged();
        }
        partial void OnInvoiceNumberChanged(string? value) => FormChanged();
        partial void OnInvoiceDateChanged(DateTime value) => FormChanged();
        partial void OnExchangeRateTextChanged(string? value) => FormChanged();
        partial void OnIncludeShippingChanged(bool value) => FormChanged();
        partial void OnShippingAmountTextChanged(string? value) => FormChanged();

        [RelayCommand]
        private void ChooseExistingSupplier() => IsNewSupplier = false;

        [RelayCommand]
        private void ChooseNewSupplier()
        {
            if (string.IsNullOrWhiteSpace(NewSupplierName))
                NewSupplierName = _extraction.SupplierName;
            IsNewSupplier = true;
        }

        // ----- lignes -----

        public void OnLineChanged(InvoiceLineItemViewModel line, string property)
        {
            if (_loading)
                return;

            // Un même article (groupKey) garde un seul nom et un seul coefficient.
            if (line.IsNew && line.GroupKey != null && property is nameof(InvoiceLineItemViewModel.Name) or nameof(InvoiceLineItemViewModel.CoefficientText))
            {
                foreach (var other in Lines.Where(l => l != line && l.IsNew && l.GroupKey == line.GroupKey))
                    other.ApplyGroupValues(line.Name, line.CoefficientText);
            }

            RefreshState();
            ScheduleDraftSave();
        }

        public void OnLineEditRequested(InvoiceLineItemViewModel line)
        {
            var open = !line.IsEditing;
            foreach (var other in Lines)
                other.IsEditing = false;
            line.IsEditing = open;
        }

        public async Task ValidateLineAsync(InvoiceLineItemViewModel line)
        {
            if (line.IsValidated)
            {
                line.SetValidated(false);
            }
            else
            {
                var problem = InvoiceReview.LineProblem(line.ToLine());
                if (problem != null)
                {
                    line.IsEditing = true;
                    await _dialogService.ShowInfoAsync(problem, "Ligne à corriger");
                    return;
                }
                line.SetValidated(true);
                line.IsEditing = false;
            }

            RefreshState();
            ScheduleDraftSave();
        }

        [RelayCommand]
        private async Task ValidateAll()
        {
            var toFix = new List<InvoiceLineItemViewModel>();
            foreach (var line in Lines.Where(l => !l.IsValidated))
            {
                if (InvoiceReview.LineProblem(line.ToLine()) != null)
                    toFix.Add(line);
                else
                    line.SetValidated(true);
            }

            foreach (var line in Lines)
                line.IsEditing = false;

            RefreshState();
            ScheduleDraftSave();

            if (toFix.Count > 0)
            {
                toFix[0].IsEditing = true;
                await _dialogService.ShowInfoAsync(
                    toFix.Count == 1
                        ? "Une ligne est à corriger avant d'être validée : elle est ouverte."
                        : $"{toFix.Count} lignes sont à corriger avant d'être validées. La première est ouverte.",
                    "Lignes à corriger");
            }
        }

        public async Task AddLinePhotoAsync(InvoiceLineItemViewModel line)
        {
            if (line.IsUploadingPhoto)
                return;

            var choices = MediaPicker.Default.IsCaptureSupported
                ? new[] { TakePhotoChoice, PickPhotoChoice }
                : new[] { PickPhotoChoice };
            var choice = await _dialogService.ChooseAsync("Photo de l'article (facultative)", choices);

            FileResult? file;
            try
            {
                file = choice switch
                {
                    TakePhotoChoice => await MediaPicker.Default.CapturePhotoAsync(),
                    PickPhotoChoice => (await MediaPicker.Default.PickPhotosAsync(new MediaPickerOptions { SelectionLimit = 1 }))?.FirstOrDefault(),
                    _ => null,
                };
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                await _dialogService.ShowInfoAsync("Impossible d'ouvrir l'appareil photo ou la galerie. Vérifiez l'autorisation dans les réglages.");
                return;
            }

            if (file == null)
                return;

            line.IsUploadingPhoto = true;
            try
            {
                byte[] bytes;
                await using (var source = await file.OpenReadAsync())
                using (var memory = new MemoryStream())
                {
                    await source.CopyToAsync(memory);
                    bytes = memory.ToArray();
                }

                using (var upload = new MemoryStream(bytes))
                    await _documents.UploadLinePhotoAsync(_documentId, line.N, new UploadFile(upload, file.FileName, ContentTypeOf(file)));

                line.SetPhoto(ImageSource.FromStream(() => new MemoryStream(bytes)));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                await _dialogService.ShowErrorAsync(e);
            }
            finally
            {
                line.IsUploadingPhoto = false;
            }
        }

        private static string ContentTypeOf(FileResult file)
        {
            var name = file.FileName.ToLowerInvariant();
            if (name.EndsWith(".png")) return "image/png";
            if (name.EndsWith(".webp")) return "image/webp";
            return "image/jpeg";
        }

        // ----- brouillon -----

        private void ScheduleDraftSave()
        {
            if (_loading || _documentId == 0 || !IsLoaded)
                return;

            _draftCts?.Cancel();
            var cts = _draftCts = new CancellationTokenSource();
            _draftPending = true;
            _ = SaveDraftLaterAsync(cts.Token);
        }

        private async Task SaveDraftLaterAsync(CancellationToken token)
        {
            try
            {
                await Task.Delay(DraftDelayMs, token);
            }
            catch (TaskCanceledException)
            {
                return;
            }
            await SaveDraftNowAsync();
            await RefreshPreviewAsync();
        }

        private async Task SaveDraftNowAsync()
        {
            await _draftLock.WaitAsync();
            try
            {
                var json = DraftJson();
                var text = json.ToString(Formatting.None);
                _draftPending = false;
                if (text == _lastDraftJson)
                    return;

                await _documents.SaveDraftAsync(_documentId, json);
                _lastDraftJson = text;
                DraftStatusText = $"Brouillon enregistré à {DateTime.Now:HH:mm}.";
            }
            catch (Exception e)
            {
                // Le brouillon sera renvoyé à la prochaine modification ; la vérification continue.
                Console.WriteLine(e);
                _draftPending = true;
                DraftStatusText = "Brouillon non enregistré : vérifiez la connexion. Il sera renvoyé à la prochaine modification.";
            }
            finally
            {
                _draftLock.Release();
            }
        }

        /// <summary>Envoie tout de suite une modification en attente (départ de l'écran, validation).</summary>
        public async Task FlushDraftAsync()
        {
            _draftCts?.Cancel();
            if (_draftPending && IsLoaded)
                await SaveDraftNowAsync();
        }

        // ----- validation de la facture -----

        [RelayCommand(CanExecute = nameof(CanConfirm))]
        private async Task Confirm() => await ConfirmAsync(classificationDate: null, force: false);

        private async Task ConfirmAsync(string? classificationDate, bool force)
        {
            var result = InvoiceConfirmationBuilder.Build(BuildDraft(), Context(classificationDate, force));
            if (!result.IsValid)
            {
                await _dialogService.ShowInfoAsync(string.Join("\n", result.Errors.Take(6)), "À compléter");
                return;
            }

            await FlushDraftAsync();

            JObject? response;
            IsBusy = true;
            try
            {
                response = await _documents.ConfirmAsync(_documentId, result.Body!);
            }
            catch (ApiException e) when (e.ApiError.Code == "INVOICE_CONFIRMATION_PENDING")
            {
                // N'existe plus depuis la fin de L1 ; gardé au cas où le serveur le renverrait.
                IsBusy = false;
                await _dialogService.ShowInfoAsync("La validation des factures fournisseur arrive très bientôt. Votre vérification est enregistrée.");
                return;
            }
            catch (ApiException e) when (e.ApiError.Code == "PERIOD_CLOSED" && classificationDate == null)
            {
                IsBusy = false;
                var inCurrent = await _dialogService.ConfirmAsync(
                    "Période clôturée",
                    "La date de cette facture tombe dans une période clôturée. L'enregistrer dans la période en cours ? Sinon, rouvrez la période depuis le tableau de bord.",
                    "Enregistrer dans la période en cours",
                    "Annuler");
                if (inCurrent)
                    await ConfirmAsync(DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), force);
                return;
            }
            catch (ApiException e) when (e.ApiError.Code == "INVOICE_ALREADY_CONFIRMED" && !force)
            {
                IsBusy = false;
                var again = await _dialogService.ConfirmAsync(
                    "Facture déjà enregistrée",
                    "Une facture de ce fournisseur avec ce numéro est déjà enregistrée. La valider quand même ?",
                    "Valider quand même",
                    "Annuler");
                if (again)
                    await ConfirmAsync(classificationDate, force: true);
                return;
            }
            catch (ApiException e) when (e.ApiError.Code == "DOCUMENT_CONFIRMED")
            {
                IsBusy = false;
                await _dialogService.ShowInfoAsync("Cette facture est déjà validée.");
                await GoBackAsync();
                return;
            }
            catch (ApiException e) when (e.ApiError.Code == "DOCUMENT_EXPIRED")
            {
                IsBusy = false;
                await _dialogService.ShowInfoAsync("Cette facture n'a pas été validée dans les 30 jours : elle a été effacée. Lisez-la à nouveau.");
                return;
            }
            catch (Exception e)
            {
                IsBusy = false;
                Console.WriteLine(e);
                await _dialogService.ShowErrorAsync(e);
                return;
            }
            finally
            {
                IsBusy = false;
            }

            var commandeId = InvoiceExtraction.Int(response?["commande"]?["id"]) ?? 0;
            if (commandeId > 0)
            {
                var receive = await _dialogService.ConfirmAsync(
                    "Facture validée",
                    "La commande fournisseur est créée. Avez-vous déjà reçu la marchandise ?",
                    "Marquer comme reçue maintenant",
                    "Plus tard");
                if (receive)
                    await ReceiveAsync(commandeId);
            }
            else
            {
                await _dialogService.ShowInfoAsync("Facture validée : la commande fournisseur est créée.");
            }

            await GoBackAsync();
        }

        private async Task ReceiveAsync(int commandeId)
        {
            // 1. Tout ce qui peut être reçu l'est ; les articles à plusieurs variantes reviennent dans needsVariant.
            var first = await TryReceiveAsync(commandeId, body: null);
            if (first == null)
                return;

            // 2. Le gérant dit quelle variante il a reçue, puis la même route reçoit ces lignes.
            var choices = new List<(int LineId, int VariantId)>();
            var postponed = 0;
            foreach (var need in first.NeedsVariant)
            {
                if (need.Variants.Count == 0)
                {
                    postponed++;
                    continue;
                }

                var labels = need.ChoiceLabels();
                var title = $"Quelle variante de « {need.ProductName ?? "cet article"} » avez-vous reçue ?";
                var choice = await ChooseAsync(title, labels);
                var index = Array.IndexOf(labels, choice);
                if (index < 0)
                    postponed++;
                else
                    choices.Add((need.LineId, need.Variants[index].Id));
            }

            var received = first.ReceivedQuantity;
            var last = first;
            if (choices.Count > 0)
            {
                var second = await TryReceiveAsync(commandeId, CommandeReception.VariantChoicesBody(choices));
                if (second != null)
                {
                    received += second.ReceivedQuantity;
                    last = second;
                }
                else
                {
                    postponed += choices.Count;
                }
            }

            await _dialogService.ShowInfoAsync(CommandeReception.Summary(received, last, postponed));
        }

        /// <summary>Un appel de réception ; null si la réception n'a pas pu se faire (message déjà affiché).</summary>
        private async Task<CommandeReception?> TryReceiveAsync(int commandeId, JObject? body)
        {
            IsBusy = true;
            try
            {
                return await _reception.ReceiveAsync(commandeId, body);
            }
            catch (ApiException e) when (e.StatusCode == HttpStatusCode.NotFound && e.ApiError.Code is not ("LINE_NOT_FOUND" or "COMMANDE_NOT_FOUND"))
            {
                IsBusy = false;
                await _dialogService.ShowInfoAsync("La réception des commandes n'est pas encore disponible. Votre facture est bien validée.");
                return null;
            }
            catch (Exception e)
            {
                // VARIANT_REQUIRED, EXCEEDS_REMAINING, NOTHING_TO_RECEIVE… : le message du serveur, par le dialogue habituel.
                IsBusy = false;
                Console.WriteLine(e);
                await _dialogService.ShowErrorAsync(e);
                return null;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private static async Task GoBackAsync()
        {
            if (Shell.Current is { } shell)
                await shell.GoToAsync("..", true);
        }

        /// <summary>Départ de l'écran : la dernière modification est envoyée.</summary>
        public void Stop() => _ = FlushDraftAsync();
    }
}
