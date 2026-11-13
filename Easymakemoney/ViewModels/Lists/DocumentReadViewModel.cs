using System.Globalization;
using Easymakemoney.Converters;
using Easymakemoney.Services.Api;
using Easymakemoney.Services.Dialog;
using Easymakemoney.UseCase.DocumentUseCase;
using Easymakemoney.Utilities;
using Newtonsoft.Json.Linq;

namespace Easymakemoney.ViewModels.Lists
{
    /// <summary>
    /// Lecture d'un reçu (note de frais) ou d'une facture de transport : photos ou PDF, lecture par le backend,
    /// vérification par le gérant, puis création. Rien n'est créé sans sa validation.
    /// </summary>
    public partial class DocumentReadViewModel : BaseViewModel
    {
        private const int PollDelayMs = 3000;
        private const int MaxPolls = 60;
        private const int MaxPages = 6;

        private readonly DocumentReaderUseCase _documents;
        private readonly IListNoteDeFraisServices _notes;
        private readonly IListTransporteurService _transporteurs;
        private readonly IDialogService _dialogService;
        private readonly List<FileResult> _files = new();
        private bool _stopped;

        public DocumentReadViewModel(DocumentReaderUseCase documents, IListNoteDeFraisServices notes,
            IListTransporteurService transporteurs, IDialogService dialogService)
        {
            _documents = documents;
            _notes = notes;
            _transporteurs = transporteurs;
            _dialogService = dialogService;
        }

        /// <summary>« note », « transport » ou « facture » (facture fournisseur, vérifiée dans InvoiceReviewPage).</summary>
        public string DocType { get; set; } = "note";
        public int CommandeId { get; set; }

        /// <summary>« campaign » : facture d'une régie (type note) qui deviendra une campagne de publicité, pas une note de frais.</summary>
        public string? Purpose { get; set; }

        public bool IsNote => DocType == "note";
        public bool IsTransport => DocType == "transport";
        public bool IsInvoice => DocType == "facture";
        public bool IsCampaign => IsNote && Purpose == "campaign";
        public bool CanEnterManually => !IsInvoice && !IsCampaign;
        public string PageTitle => DocType switch
        {
            "note" when IsCampaign => "Lire la facture de la régie",
            "note" => "Lire un reçu",
            "facture" => "Lire une facture fournisseur",
            _ => "Lire une facture de transport",
        };

        public string IntroText => IsInvoice
            ? "Photographiez la facture du fournisseur ou choisissez des photos (6 pages au plus), ou un PDF. Vous vérifierez ensuite chaque ligne : rien n'est enregistré avant votre validation."
            : IsCampaign
                ? "Photographiez la facture de la régie (Facebook, Google, journal, imprimeur…) ou choisissez des photos (6 pages au plus), ou un PDF. Vous vérifierez ensuite la campagne : rien n'est enregistré avant votre validation."
                : "Photographiez le document ou choisissez des photos (6 pages au plus), ou un PDF. Rien n'est enregistré avant votre validation.";

        /// <summary>Factures déjà lues et pas encore validées : le gérant reprend sa vérification.</summary>
        public ObservableCollection<PendingInvoice> PendingInvoices { get; } = new();

        [ObservableProperty] private bool hasPendingInvoices;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsChoosing))]
        [NotifyPropertyChangedFor(nameof(IsReading))]
        [NotifyPropertyChangedFor(nameof(IsVerifying))]
        [NotifyPropertyChangedFor(nameof(IsFailed))]
        private string step = "choose";

        public bool IsChoosing => Step == "choose";
        public bool IsReading => Step == "reading";
        public bool IsVerifying => Step == "verify";
        public bool IsFailed => Step == "failed";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasPages))]
        private string pagesText = "Aucune page";

        public bool HasPages => _files.Count > 0;

        [ObservableProperty] private string? failureText;
        [ObservableProperty] private string? remarksText;
        [ObservableProperty] private bool hasRemarks;

        // Champs vérifiés par le gérant (texte : la virgule doit rester saisissable)
        [ObservableProperty] private string? amountText;
        [ObservableProperty] private DateTime date = DateTime.Today;
        [ObservableProperty] private string? description;
        [ObservableProperty] private string? merchant;
        [ObservableProperty] private string? trackingNumber;
        [ObservableProperty] private string? invoiceNumber;
        [ObservableProperty] private TypeNoteDeFrais? selectedType;
        [ObservableProperty] private Transporteur? selectedTransporteur;

        public ObservableCollection<TypeNoteDeFrais> Types { get; } = new();
        public ObservableCollection<Transporteur> Transporteurs { get; } = new();

        public string AmountLabel => IsNote ? "Montant toutes taxes" : "Montant hors taxes";

        private int _documentId;

        public void Stop() => _stopped = true;

        /// <summary>Attente entre deux relectures du document (3 s) ; les tests la mettent à zéro.</summary>
        public TimeSpan PollDelay { get; set; } = TimeSpan.FromMilliseconds(PollDelayMs);

        /// <summary>Navigation (point d'entrée des tests).</summary>
        public Func<string, Task> NavigateAsync { get; set; } = route => Shell.Current?.GoToAsync(route, true) ?? Task.CompletedTask;

        /// <summary>Lot IA-FACT-1 : une facture fournisseur se mène en conversation, ouverte dès l'envoi des photos.</summary>
        private static string ConversationRoute(int documentId) => $"../{nameof(InvoiceConversationPage)}?DocumentId={documentId}";

        /// <summary>Ouverture d'une page choisie (photo ou PDF) ; les tests la remplacent, car la version de bureau de MAUI ne sait pas lire un FileResult.</summary>
        public Func<FileResult, Task<Stream>> OpenFile { get; set; } = file => file.OpenReadAsync();

        public async Task InitAsync(string type, int commandeId, string? purpose = null)
        {
            DocType = type;
            CommandeId = commandeId;
            Purpose = purpose;
            _stopped = false;
            OnPropertyChanged(nameof(IsNote));
            OnPropertyChanged(nameof(IsTransport));
            OnPropertyChanged(nameof(IsInvoice));
            OnPropertyChanged(nameof(IsCampaign));
            OnPropertyChanged(nameof(CanEnterManually));
            OnPropertyChanged(nameof(PageTitle));
            OnPropertyChanged(nameof(IntroText));
            OnPropertyChanged(nameof(AmountLabel));

            if (IsInvoice)
                await LoadPendingInvoicesAsync();

            try
            {
                if (IsNote && !IsCampaign && Types.Count == 0)
                {
                    foreach (var item in await _notes.GetTypeNoteDeFrais())
                        Types.Add(item);
                }
                if (IsTransport && Transporteurs.Count == 0)
                {
                    foreach (var item in await _transporteurs.GetListTransporteur())
                        Transporteurs.Add(item);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                await _dialogService.ShowErrorAsync(e);
            }
        }

        private async Task LoadPendingInvoicesAsync()
        {
            try
            {
                var documents = await _documents.ListAsync("facture", "extracted");
                PendingInvoices.Clear();
                foreach (var document in documents.Where(d => d.id > 0))
                    PendingInvoices.Add(new PendingInvoice(document.id, PendingInvoice.TextFor(document.createdAt)));
            }
            catch (Exception e)
            {
                // Sans la liste, le gérant peut toujours lire une nouvelle facture.
                Console.WriteLine(e);
            }
            HasPendingInvoices = PendingInvoices.Count > 0;
        }

        [RelayCommand]
        private async Task ResumeInvoice(PendingInvoice? invoice)
        {
            if (invoice == null)
                return;
            await NavigateAsync(ConversationRoute(invoice.Id));
        }

        [RelayCommand]
        private async Task TakePhoto()
        {
            if (!MediaPicker.Default.IsCaptureSupported)
            {
                await _dialogService.ShowInfoAsync("L'appareil photo n'est pas disponible. Choisissez une photo existante.");
                return;
            }

            try
            {
                var photo = await MediaPicker.Default.CapturePhotoAsync();
                if (photo != null)
                    AddFiles(new[] { photo });
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                await _dialogService.ShowInfoAsync("Impossible d'utiliser l'appareil photo. Vérifiez l'autorisation dans les réglages.");
            }
        }

        [RelayCommand]
        private async Task PickPhotos()
        {
            try
            {
                // Galerie photo du téléphone, plusieurs pages à la fois.
                var picked = await MediaPicker.Default.PickPhotosAsync(new MediaPickerOptions { SelectionLimit = MaxPages });
                if (picked != null)
                    AddFiles(picked.Where(f => f != null)!);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                await _dialogService.ShowInfoAsync("Impossible d'ouvrir la galerie. Vérifiez l'autorisation d'accès aux photos dans les réglages.");
            }
        }

        [RelayCommand]
        private async Task PickPdf()
        {
            try
            {
                var pdf = await FilePicker.Default.PickAsync(new PickOptions { FileTypes = FilePickerFileType.Pdf });
                if (pdf != null)
                {
                    _files.Clear();
                    AddFiles(new[] { pdf });
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                await _dialogService.ShowInfoAsync("Impossible d'ouvrir ce PDF. Choisissez un autre fichier.");
            }
        }

        [RelayCommand]
        private void ClearPages()
        {
            _files.Clear();
            RefreshPages();
        }

        /// <summary>Ajoute des pages (photos ou un PDF), comme le font l'appareil photo et les sélecteurs.</summary>
        public void AddFiles(IEnumerable<FileResult> files)
        {
            // Un PDF se lit seul ; des photos ne se mélangent pas avec un PDF.
            if (_files.Any(IsPdf))
                _files.Clear();

            foreach (var file in files)
            {
                if (_files.Count >= MaxPages)
                    break;
                _files.Add(file);
            }
            RefreshPages();
        }

        private void RefreshPages()
        {
            PagesText = _files.Count switch
            {
                0 => "Aucune page",
                1 when IsPdf(_files[0]) => "1 document PDF",
                1 => "1 page",
                var n => $"{n} pages"
            };
            OnPropertyChanged(nameof(HasPages));
        }

        private static bool IsPdf(FileResult file) =>
            file.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase);

        private static string ContentTypeOf(FileResult file)
        {
            var name = file.FileName.ToLowerInvariant();
            if (name.EndsWith(".pdf")) return "application/pdf";
            if (name.EndsWith(".png")) return "image/png";
            if (name.EndsWith(".webp")) return "image/webp";
            return "image/jpeg";
        }

        [RelayCommand]
        private async Task Read()
        {
            if (IsBusy || _files.Count == 0)
                return;

            IsBusy = true;
            var streams = new List<Stream>();
            try
            {
                var uploads = new List<UploadFile>();
                foreach (var file in _files)
                {
                    var stream = await OpenFile(file);
                    streams.Add(stream);
                    uploads.Add(new UploadFile(stream, file.FileName, ContentTypeOf(file)));
                }

                var document = await _documents.UploadAsync(DocType, uploads);
                _documentId = document.id;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                await _dialogService.ShowErrorAsync(e);
                return;
            }
            finally
            {
                foreach (var stream in streams)
                    stream.Dispose();
                IsBusy = false;
            }

            // La conversation suit elle-même la lecture, pose les questions et crée la commande.
            if (IsInvoice)
            {
                await NavigateAsync(ConversationRoute(_documentId));
                return;
            }
            await WaitForReadingAsync();
        }

        [RelayCommand]
        private async Task Reread()
        {
            if (_documentId == 0 || IsBusy)
                return;

            // Une relecture consomme une unité IA du site : le gérant la confirme, comme sur le web.
            if (!await _dialogService.ConfirmAsync("Relire le document",
                    "Relancer la lecture par l'IA ? Cela consomme une unité IA du mois.", "Relire", "Annuler"))
                return;

            try
            {
                await _documents.RereadAsync(_documentId);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                await _dialogService.ShowErrorAsync(e);
                return;
            }

            await WaitForReadingAsync();
        }

        private async Task WaitForReadingAsync()
        {
            Step = "reading";
            for (var attempt = 0; attempt < MaxPolls && !_stopped; attempt++)
            {
                await Task.Delay(PollDelay);
                if (_stopped)
                    return;

                GestionDocument document;
                try
                {
                    document = await _documents.GetAsync(_documentId);
                }
                catch (Exception e)
                {
                    // Réseau faible : on réessaie au prochain tour, la lecture continue sur le serveur.
                    Console.WriteLine(e);
                    continue;
                }

                switch (document.status)
                {
                    case "extracted" when IsInvoice:
                        // Une facture fournisseur se vérifie ligne par ligne sur son propre écran, qui remplace celui-ci.
                        if (Shell.Current is { } shell)
                            await shell.GoToAsync($"../{nameof(InvoiceReviewPage)}?DocumentId={_documentId}", true);
                        return;
                    case "extracted" when IsCampaign:
                        // La facture d'une régie se vérifie dans le formulaire de campagne qui a ouvert cet écran :
                        // il reçoit le document, se préremplit et le confirmera en campagne.
                        if (Shell.Current is { } campaignShell)
                            await campaignShell.GoToAsync($"..?DocumentId={_documentId}", true);
                        return;
                    case "extracted":
                        Prefill(document.extraction);
                        Step = "verify";
                        return;
                    case "failed":
                        FailureText = (document.error, IsInvoice || IsCampaign) switch
                        {
                            ("DOCUMENT_UNREADABLE", true) => "La facture est illisible. Reprenez une photo plus nette.",
                            ("AI_TOO_MANY_FAILURES", true) => "La lecture a échoué plusieurs fois. Réessayez plus tard.",
                            ("DOCUMENT_UNREADABLE", _) => "Le document est illisible. Reprenez une photo plus nette, ou saisissez les informations à la main.",
                            ("AI_TOO_MANY_FAILURES", _) => "La lecture a échoué plusieurs fois. Réessayez plus tard, ou saisissez les informations à la main.",
                            _ => "Le service de lecture est indisponible pour le moment. Aucun crédit n'a été utilisé.",
                        };
                        Step = "failed";
                        return;
                    case "expired":
                    case "confirmed":
                        Step = "choose";
                        return;
                }
            }

            if (!_stopped)
            {
                FailureText = IsInvoice
                    ? "La lecture prend plus de temps que prévu. Revenez plus tard : la facture sera proposée en haut de cet écran."
                    : IsCampaign
                        ? "La lecture prend plus de temps que prévu. Revenez en arrière pour saisir la campagne à la main."
                        : "La lecture prend plus de temps que prévu. Revenez plus tard ou saisissez les informations à la main.";
                Step = "failed";
            }
        }

        [RelayCommand]
        private void ManualEntry()
        {
            if (IsInvoice || IsCampaign)
                return;

            Prefill(null);
            Step = "verify";
        }

        private void Prefill(JObject? extraction)
        {
            var amount = extraction?["amount"]?.Value<long?>();
            AmountText = amount.HasValue ? (amount.Value / 100m).ToString("0.00", CultureInfo.CurrentCulture) : null;

            var remarks = extraction?["notes"]?.Values<string>().Where(n => !string.IsNullOrWhiteSpace(n)).ToList() ?? new List<string?>();
            var confidence = extraction?["confidence"]?["overall"]?.Value<double?>();
            if (confidence is < 0.7)
                remarks.Insert(0, "Lecture incertaine : vérifiez chaque champ.");
            if (extraction?["duplicate"] is JObject)
                remarks.Insert(0, "Ce document semble déjà enregistré.");
            RemarksText = string.Join("\n", remarks);
            HasRemarks = remarks.Count > 0;

            if (IsNote)
            {
                Date = ParseDate(extraction?["date"]?.Value<string>()) ?? DateTime.Today;
                Merchant = extraction?["merchant"]?.Value<string>();
                Description = extraction?["description"]?.Value<string>() ?? Merchant;
                var typeId = extraction?["typeNoteDeFrais"]?["id"]?.Value<int?>();
                SelectedType = Types.FirstOrDefault(t => t.id == typeId);
            }
            else
            {
                Date = ParseDate(extraction?["invoice"]?["date"]?.Value<string>()) ?? DateTime.Today;
                InvoiceNumber = extraction?["invoice"]?["number"]?.Value<string>();
                TrackingNumber = extraction?["trackingNumber"]?.Value<string>();
                var carrierId = extraction?["carrier"]?["id"]?.Value<int?>();
                SelectedTransporteur = Transporteurs.FirstOrDefault(t => t.id == carrierId);
            }
        }

        private static DateTime? ParseDate(string? text) =>
            DateTime.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
                ? date
                : null;

        [RelayCommand]
        private async Task Confirm()
        {
            if (IsBusy)
                return;

            var amount = DecimalInputConverter.Parse(AmountText);
            if (amount is null or < 0)
            {
                await _dialogService.ShowInfoAsync("Montant invalide.");
                return;
            }
            if (IsNote && SelectedType == null)
            {
                await _dialogService.ShowInfoAsync("Choisissez le type de la note de frais.");
                return;
            }
            if (IsTransport && SelectedTransporteur == null)
            {
                await _dialogService.ShowInfoAsync("Choisissez le transporteur.");
                return;
            }

            await ConfirmAsync(MoneyHelper.ToCents(amount), classificationDate: null, force: false);
        }

        private async Task ConfirmAsync(long cents, string? classificationDate, bool force)
        {
            if (_documentId == 0)
            {
                await _dialogService.ShowInfoAsync("Lisez d'abord un document : il servira de justificatif.");
                return;
            }

            object body = IsNote
                ? new Dictionary<string, object?>
                {
                    ["montant"] = cents,
                    ["date"] = Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    ["classificationDate"] = classificationDate,
                    ["description"] = Description ?? string.Empty,
                    ["typeNoteDeFraisId"] = SelectedType!.id,
                    ["name"] = string.IsNullOrWhiteSpace(Merchant) ? null : Merchant,
                }.Where(kv => kv.Value != null).ToDictionary(kv => kv.Key, kv => kv.Value)
                : new Dictionary<string, object?>
                {
                    ["commandeId"] = CommandeId,
                    ["transporteurId"] = SelectedTransporteur!.id,
                    ["price"] = cents,
                    ["tracknumber"] = string.IsNullOrWhiteSpace(TrackingNumber) ? null : TrackingNumber,
                    ["facture"] = string.IsNullOrWhiteSpace(InvoiceNumber) ? null : InvoiceNumber,
                    ["force"] = force ? true : null,
                }.Where(kv => kv.Value != null).ToDictionary(kv => kv.Key, kv => kv.Value);

            IsBusy = true;
            try
            {
                await _documents.ConfirmAsync(_documentId, body);
            }
            catch (ApiException e) when (e.ApiError.Code == "PERIOD_CLOSED" && IsNote && classificationDate == null)
            {
                IsBusy = false;
                var inCurrent = await _dialogService.ConfirmAsync(
                    "Période clôturée",
                    "La date de ce reçu tombe dans une période clôturée. L'enregistrer dans la période en cours ? Sinon, rouvrez la période depuis le tableau de bord.",
                    "Période en cours",
                    "Annuler");
                if (inCurrent)
                    await ConfirmAsync(cents, DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), force);
                return;
            }
            catch (ApiException e) when (e.ApiError.Code == "INVOICE_ALREADY_CONFIRMED" && !force)
            {
                IsBusy = false;
                var again = await _dialogService.ConfirmAsync(
                    "Facture déjà enregistrée",
                    "Une facture de ce transporteur avec ce numéro existe déjà. L'enregistrer quand même ?",
                    "Enregistrer",
                    "Annuler");
                if (again)
                    await ConfirmAsync(cents, classificationDate, force: true);
                return;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                await _dialogService.ShowErrorAsync(e);
                return;
            }
            finally
            {
                IsBusy = false;
            }

            await _dialogService.ShowInfoAsync(IsNote ? "Note de frais enregistrée." : "Frais de port enregistré.");
            if (Shell.Current is { } shell)
                await shell.GoToAsync("..", true);
        }
    }

    /// <summary>Facture fournisseur lue, en attente de vérification.</summary>
    public sealed class PendingInvoice
    {
        public PendingInvoice(int id, string text)
        {
            Id = id;
            Text = text;
        }

        public int Id { get; }
        public string Text { get; }

        public static string TextFor(string? createdAt) =>
            DateTimeOffset.TryParse(createdAt, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
                ? $"Facture lue le {date.ToLocalTime():dd/MM/yyyy à HH:mm}"
                : "Facture lue, à vérifier";
    }
}
