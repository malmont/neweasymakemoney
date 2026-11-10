using System.Globalization;
using Easymakemoney.Services.Api;
using Easymakemoney.Services.Dialog;
using Easymakemoney.UseCase.DocumentUseCase;
using Easymakemoney.UseCase.ProductAttachmentUseCase;

namespace Easymakemoney.ViewModels.Lists
{
    /// <summary>Un article ciblé par la campagne.</summary>
    public partial class CampaignTargetProduct : ObservableObject
    {
        public CampaignTargetProduct(int id, string? name)
        {
            Id = id;
            Name = string.IsNullOrWhiteSpace(name) ? $"Article n° {id}" : name;
        }

        public int Id { get; }

        [ObservableProperty] private string name = string.Empty;
    }

    /// <summary>
    /// Création et modification d'une campagne de publicité. Avec un justificatif lu (facture de la régie),
    /// la validation confirme le document en campagne au lieu de créer la campagne directement.
    /// </summary>
    public partial class CampaignFormViewModel : BaseViewModel
    {
        public const string SiteCurrencyChoice = "Devise du site";
        private const int SearchDelayMs = 300;
        private const int SearchLimit = 20;

        private readonly ICampaignService _campaigns;
        private readonly DocumentReaderUseCase _documents;
        private readonly GetAvailableProductsUseCase _products;
        private readonly IDialogService _dialogService;

        private CancellationTokenSource? _searchDelay;
        private int _searchVersion;
        private string? _siteCurrency;

        public CampaignFormViewModel(ICampaignService campaigns, DocumentReaderUseCase documents,
            GetAvailableProductsUseCase products, IDialogService dialogService)
        {
            _campaigns = campaigns;
            _documents = documents;
            _products = products;
            _dialogService = dialogService;
        }

        // ----- mode -----

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsEdit))]
        [NotifyPropertyChangedFor(nameof(PageTitle))]
        [NotifyPropertyChangedFor(nameof(CanReadInvoice))]
        private int campaignId;

        /// <summary>Facture de régie lue, à confirmer en campagne ; 0 sans justificatif.</summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasDocument))]
        [NotifyPropertyChangedFor(nameof(CanReadInvoice))]
        private int documentId;

        public bool IsEdit => CampaignId > 0;
        public bool HasDocument => DocumentId > 0;
        public bool CanReadInvoice => !IsEdit && !HasDocument;
        public string PageTitle => IsEdit ? "Modifier la campagne" : "Nouvelle campagne";

        [ObservableProperty] private bool isLoaded;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasLoadError))]
        private string? loadError;

        public bool HasLoadError => !string.IsNullOrEmpty(LoadError);

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasDocumentText))]
        private string? documentText;

        public bool HasDocumentText => !string.IsNullOrEmpty(DocumentText);

        [ObservableProperty] private string? remarksText;
        [ObservableProperty] private bool hasRemarks;
        [ObservableProperty] private string? commandesText;
        [ObservableProperty] private bool hasCommandes;

        // ----- champs -----

        [ObservableProperty] private string? name;

        public IReadOnlyList<CampaignChannelChoice> Channels => CampaignChannels.All;

        [ObservableProperty] private CampaignChannelChoice? selectedChannel;

        [ObservableProperty] private DateTime startDate = DateTime.Today;
        [ObservableProperty] private DateTime endDate = DateTime.Today;

        // Texte : la virgule doit rester saisissable (« 12,50 »).
        [ObservableProperty] private string? amountText;

        public ObservableCollection<string> CurrencyChoices { get; } = new() { SiteCurrencyChoice, "CAD", "EUR", "USD" };

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(NeedsExchangeRate))]
        [NotifyPropertyChangedFor(nameof(ExchangeRateLabel))]
        [NotifyPropertyChangedFor(nameof(AmountLabel))]
        private string selectedCurrency = SiteCurrencyChoice;

        [ObservableProperty] private string? exchangeRateText;
        [ObservableProperty] private string? notes;

        /// <summary>Code de la devise choisie ; null : devise du site.</summary>
        public string? CurrencyCode => SelectedCurrency == SiteCurrencyChoice ? null : SelectedCurrency;

        public bool NeedsExchangeRate => CampaignRequestBuilder.NeedsExchangeRate(CurrencyCode, _siteCurrency);

        public string ExchangeRateLabel => NeedsExchangeRate
            ? CampaignRequestBuilder.ExchangeRateLabel(CurrencyCode, _siteCurrency)
            : string.Empty;

        public string AmountLabel => CurrencyCode == null ? "Montant" : $"Montant ({CurrencyCode})";

        partial void OnStartDateChanged(DateTime value)
        {
            if (EndDate < value)
                EndDate = value;
        }

        // ----- ciblage d'articles -----

        [ObservableProperty] private bool isTargeting;
        [ObservableProperty] private string? productSearchText;
        [ObservableProperty] private string? searchStatusText;

        public ObservableCollection<AvailableProduct> SearchResults { get; } = new();
        public ObservableCollection<CampaignTargetProduct> TargetProducts { get; } = new();

        [ObservableProperty] private string targetSummary = "Toutes les ventes du magasin";

        partial void OnIsTargetingChanged(bool value)
        {
            if (value)
            {
                _ = SearchProductsAsync();
            }
            else
            {
                TargetProducts.Clear();
                SearchResults.Clear();
                RefreshTargets();
            }
        }

        partial void OnProductSearchTextChanged(string? value) => _ = SearchAfterDelayAsync();

        private async Task SearchAfterDelayAsync()
        {
            _searchDelay?.Cancel();
            var delay = _searchDelay = new CancellationTokenSource();
            try
            {
                await Task.Delay(SearchDelayMs, delay.Token);
            }
            catch (TaskCanceledException)
            {
                return;
            }

            await SearchProductsAsync();
        }

        private async Task SearchProductsAsync()
        {
            if (!IsTargeting)
                return;

            var version = ++_searchVersion;
            SearchStatusText = "Recherche…";
            try
            {
                var page = await _products.ExecuteAsync(ProductSearchText, 1, SearchLimit, allProducts: true);
                if (version != _searchVersion)
                    return;

                SearchResults.Clear();
                foreach (var product in page.items)
                {
                    product.IsSelected = TargetProducts.Any(t => t.Id == product.id);
                    // Un article ciblé dont on ne connaissait que le numéro prend son nom.
                    if (TargetProducts.FirstOrDefault(t => t.Id == product.id) is { } target)
                        target.Name = product.name;
                    SearchResults.Add(product);
                }
                SearchStatusText = SearchResults.Count == 0 ? "Aucun article trouvé." : null;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                if (version == _searchVersion)
                    SearchStatusText = "Recherche impossible. Vérifiez la connexion.";
            }
        }

        [RelayCommand]
        private void ToggleProduct(AvailableProduct? product)
        {
            if (product == null)
                return;

            var existing = TargetProducts.FirstOrDefault(t => t.Id == product.id);
            if (existing != null)
            {
                TargetProducts.Remove(existing);
                product.IsSelected = false;
            }
            else
            {
                TargetProducts.Add(new CampaignTargetProduct(product.id, product.name));
                product.IsSelected = true;
            }
            RefreshTargets();
        }

        [RelayCommand]
        private void RemoveTarget(CampaignTargetProduct? target)
        {
            if (target == null)
                return;

            TargetProducts.Remove(target);
            foreach (var product in SearchResults.Where(p => p.id == target.Id))
                product.IsSelected = false;
            RefreshTargets();
        }

        private void RefreshTargets() => TargetSummary = TargetProducts.Count switch
        {
            0 => "Toutes les ventes du magasin",
            1 => "1 article ciblé",
            var n => $"{n} articles ciblés",
        };

        // ----- ouverture -----

        public async Task InitAsync(int campaignId, int documentId, int fromCampaignId = 0)
        {
            CampaignId = campaignId;
            _siteCurrency = SiteCurrency.CurrentCurrency;
            RefreshCurrency();

            if (campaignId > 0)
                await LoadCampaignAsync();
            else if (fromCampaignId > 0)
                await CopyCampaignAsync(fromCampaignId);
            else
                IsLoaded = true;

            if (documentId > 0)
                await UseDocumentAsync(documentId);
        }

        [RelayCommand]
        private async Task Reload() => await LoadCampaignAsync();

        private async Task LoadCampaignAsync()
        {
            if (IsBusy || CampaignId == 0)
                return;

            IsBusy = true;
            LoadError = null;
            try
            {
                var campaign = await _campaigns.GetAsync(CampaignId);
                if (campaign == null)
                {
                    LoadError = "Campagne introuvable.";
                    return;
                }
                Apply(campaign);
                IsLoaded = true;
            }
            catch (ApiException e) when (e.ApiError.Code == "CAMPAIGN_NOT_FOUND")
            {
                LoadError = "Cette campagne n'existe plus.";
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                LoadError = "Impossible d'ouvrir la campagne. Vérifiez la connexion et réessayez.";
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Relance conseillée par le guide : nouvelle campagne préremplie avec le canal, le montant, la devise et le ciblage
        /// d'une campagne rentable, sur la même durée à partir d'aujourd'hui. Rien n'est enregistré avant « Enregistrer ».
        /// </summary>
        private async Task CopyCampaignAsync(int fromCampaignId)
        {
            IsBusy = true;
            try
            {
                var source = await _campaigns.GetAsync(fromCampaignId);
                if (source != null)
                {
                    var start = CampaignDates.Parse(source.startDate) ?? DateTime.Today;
                    var end = CampaignDates.Parse(source.endDate) ?? start;
                    Apply(source);
                    Name = $"{source.name} (relance)";
                    StartDate = DateTime.Today;
                    EndDate = DateTime.Today.Add(end.Date - start.Date);
                    HasCommandes = false;
                    DocumentText = null;
                }
            }
            catch (Exception e)
            {
                // Campagne d'origine introuvable : le formulaire reste vide, rien de bloquant.
                Console.WriteLine(e);
            }
            finally
            {
                IsBusy = false;
                IsLoaded = true;
            }
        }

        private void Apply(Campaign campaign)
        {
            if (!string.IsNullOrWhiteSpace(campaign.siteCurrency))
                _siteCurrency = campaign.siteCurrency.Trim().ToUpperInvariant();

            Name = campaign.name;
            SelectedChannel = CampaignChannels.Find(campaign.channel) ?? CampaignChannels.Find(CampaignChannels.Other);
            StartDate = CampaignDates.Parse(campaign.startDate) ?? DateTime.Today;
            EndDate = CampaignDates.Parse(campaign.endDate) ?? StartDate;
            AmountText = CampaignRequestBuilder.FormatAmount(campaign.amount, CultureInfo.CurrentCulture);
            Notes = campaign.notes;

            SelectCurrency(campaign.IsForeign ? campaign.currency : null);
            ExchangeRateText = campaign.exchangeRate is > 0
                ? CampaignRequestBuilder.FormatRate(campaign.exchangeRate.Value, CultureInfo.CurrentCulture)
                : null;

            TargetProducts.Clear();
            foreach (var id in campaign.productIds.Distinct())
                TargetProducts.Add(new CampaignTargetProduct(id, null));
            RefreshTargets();
            IsTargeting = TargetProducts.Count > 0;

            // Le ciblage par commandes n'est pas modifiable dans cette version : il est conservé tel quel.
            HasCommandes = campaign.commandeIds.Count > 0;
            CommandesText = campaign.commandeIds.Count == 1
                ? "Cette campagne cible aussi les articles d'1 commande : ce ciblage est conservé."
                : $"Cette campagne cible aussi les articles de {campaign.commandeIds.Count} commandes : ce ciblage est conservé.";

            DocumentText = campaign.document != null
                ? string.IsNullOrWhiteSpace(campaign.document.invoiceNumber)
                    ? "Justificatif joint."
                    : $"Justificatif joint : facture {campaign.document.invoiceNumber}."
                : null;
        }

        private void SelectCurrency(string? code)
        {
            code = string.IsNullOrWhiteSpace(code) ? null : code.Trim().ToUpperInvariant();
            if (code != null && !CurrencyChoices.Contains(code))
                CurrencyChoices.Add(code);
            SelectedCurrency = code ?? SiteCurrencyChoice;
            RefreshCurrency();
        }

        private void RefreshCurrency()
        {
            OnPropertyChanged(nameof(NeedsExchangeRate));
            OnPropertyChanged(nameof(ExchangeRateLabel));
            OnPropertyChanged(nameof(AmountLabel));
        }

        // ----- facture de la régie -----

        [RelayCommand]
        private async Task ReadInvoice()
        {
            if (Shell.Current is { } shell)
                await shell.GoToAsync($"{nameof(DocumentReadPage)}?Type=note&Purpose=campaign");
        }

        /// <summary>Facture lue par DocumentReadPage : le formulaire est prérempli, la validation confirmera ce document.</summary>
        public async Task UseDocumentAsync(int documentId)
        {
            // Déjà prérempli avec ce document : la saisie du gérant n'est pas écrasée.
            if (documentId <= 0 || IsEdit || documentId == DocumentId)
                return;

            GestionDocument document;
            try
            {
                document = await _documents.GetAsync(documentId);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                await _dialogService.ShowErrorAsync(e);
                return;
            }

            if (document?.status != "extracted")
            {
                await _dialogService.ShowInfoAsync(document?.status switch
                {
                    "confirmed" => "Cette facture est déjà enregistrée.",
                    "expired" => "Cette facture n'a pas été validée à temps : elle a été effacée. Lisez-la à nouveau.",
                    "reading" => "La lecture de la facture n'est pas terminée. Réessayez dans un instant.",
                    _ => "La lecture de la facture a échoué. Saisissez la campagne à la main.",
                });
                return;
            }

            // Le document rend la devise du site à sa racine (lue par HttpService).
            _siteCurrency = SiteCurrency.CurrentCurrency ?? _siteCurrency;
            var prefill = CampaignDocumentPrefill.Read(document.extraction, _siteCurrency, CultureInfo.CurrentCulture);

            DocumentId = documentId;
            if (prefill.AmountText != null)
                AmountText = prefill.AmountText;
            if (prefill.Name != null)
                Name = prefill.Name;
            if (prefill.Channel != null)
                SelectedChannel = CampaignChannels.Find(prefill.Channel);
            if (prefill.Date is { } date)
            {
                StartDate = date;
                EndDate = date;
            }
            SelectCurrency(prefill.Currency);

            var remarks = new List<string>(prefill.Remarks)
            {
                "Vérifiez les dates de diffusion : la facture ne donne souvent qu'une date.",
            };
            RemarksText = string.Join("\n", remarks);
            HasRemarks = remarks.Count > 0;
            DocumentText = "Facture de la régie lue : elle sera jointe à la campagne comme justificatif.";
        }

        // ----- validation -----

        [RelayCommand]
        private async Task Save()
        {
            if (IsBusy || !IsLoaded)
                return;

            var input = new CampaignFormInput
            {
                Name = Name,
                Channel = SelectedChannel?.Code,
                StartDate = StartDate,
                EndDate = EndDate,
                AmountText = AmountText,
                Currency = CurrencyCode,
                SiteCurrency = _siteCurrency,
                ExchangeRateText = ExchangeRateText,
                ProductIds = TargetProducts.Select(t => t.Id).ToList(),
                Notes = Notes,
            };

            var request = HasDocument
                ? CampaignRequestBuilder.BuildConfirmation(input)
                : CampaignRequestBuilder.Build(input, IsEdit);
            if (!request.IsValid)
            {
                await _dialogService.ShowInfoAsync(string.Join("\n", request.Errors), "À compléter");
                return;
            }

            IsBusy = true;
            try
            {
                if (HasDocument)
                    await _documents.ConfirmAsync(DocumentId, request.Body!);
                else if (IsEdit)
                    await _campaigns.UpdateAsync(CampaignId, request.Body!);
                else
                    await _campaigns.CreateAsync(request.Body!);
            }
            catch (ApiException e) when (CampaignRequestBuilder.MessageFor(e.ApiError.Code) is { } message)
            {
                await _dialogService.ShowInfoAsync(message, "Enregistrement impossible");
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

            await _dialogService.ShowInfoAsync(IsEdit ? "Campagne modifiée." : "Campagne enregistrée.");
            if (Shell.Current is { } shell)
                await shell.GoToAsync("..", true);
        }
    }
}
