using Easymakemoney.Services.Api;
using Easymakemoney.Services.Dialog;

namespace Easymakemoney.ViewModels.Lists
{
    /// <summary>Liste des campagnes de publicité, les plus récentes d'abord, chargée page par page.</summary>
    public partial class CampaignListViewModel : BaseViewModel
    {
        private const int PageSize = 50;

        private readonly ICampaignService _campaigns;
        private readonly IDialogService _dialogService;
        private int _loadVersion;
        private int _page;
        private bool _hasMore;
        private bool _isLoadingMore;

        public CampaignListViewModel(ICampaignService campaigns, IDialogService dialogService)
        {
            _campaigns = campaigns;
            _dialogService = dialogService;
        }

        public ObservableCollection<Campaign> Campaigns { get; } = new();

        [ObservableProperty] private bool showEmptyState;
        [ObservableProperty] private string emptyTitle = "Chargement…";
        [ObservableProperty] private string? emptyText;

        public async Task LoadFirstPageAsync()
        {
            var version = ++_loadVersion;
            EmptyTitle = "Chargement…";
            EmptyText = null;
            ShowEmptyState = Campaigns.Count == 0;

            try
            {
                var items = await _campaigns.ListAsync(1, PageSize);
                if (version != _loadVersion)
                    return;

                Campaigns.Clear();
                Add(items);
                _page = 1;
                _hasMore = items.Count >= PageSize;
                EmptyTitle = "Aucune campagne";
                EmptyText = "Touchez « Nouvelle campagne » pour enregistrer une publicité.";
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                if (version != _loadVersion)
                    return;

                EmptyTitle = "Chargement impossible";
                EmptyText = "Vérifiez la connexion, puis revenez sur cet écran.";
                await _dialogService.ShowErrorAsync(e);
            }
            finally
            {
                if (version == _loadVersion)
                    ShowEmptyState = Campaigns.Count == 0;
            }
        }

        [RelayCommand]
        private async Task LoadMore()
        {
            // Le backend rend X-Total-Count, mais HttpService ne lit pas les en-têtes : une page pleine annonce la suivante.
            if (_isLoadingMore || _page == 0 || !_hasMore)
                return;

            _isLoadingMore = true;
            var version = _loadVersion;
            try
            {
                var items = await _campaigns.ListAsync(_page + 1, PageSize);
                if (version != _loadVersion)
                    return;

                Add(items);
                _page++;
                _hasMore = items.Count >= PageSize;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                await _dialogService.ShowErrorAsync(e);
            }
            finally
            {
                _isLoadingMore = false;
            }
        }

        private void Add(IEnumerable<Campaign> items)
        {
            // Une campagne créée entre deux pages décale la liste : pas de doublon.
            var known = Campaigns.Select(c => c.id).ToHashSet();
            foreach (var campaign in items)
            {
                if (known.Add(campaign.id))
                    Campaigns.Add(campaign);
            }
        }

        [RelayCommand]
        private async Task NewCampaign()
        {
            if (Shell.Current is { } shell)
                await shell.GoToAsync(nameof(CampaignFormPage));
        }

        [RelayCommand]
        private async Task Open(Campaign? campaign)
        {
            if (campaign != null && Shell.Current is { } shell)
                await shell.GoToAsync($"{nameof(CampaignProfitabilityPage)}?CampaignId={campaign.id}");
        }

        [RelayCommand]
        private async Task Edit(Campaign? campaign)
        {
            if (campaign != null && Shell.Current is { } shell)
                await shell.GoToAsync($"{nameof(CampaignFormPage)}?CampaignId={campaign.id}");
        }

        [RelayCommand]
        private async Task Delete(Campaign? campaign)
        {
            if (campaign == null || IsBusy)
                return;

            var confirmed = await _dialogService.ConfirmAsync(
                "Supprimer la campagne",
                $"Supprimer « {campaign.name} » ? Son montant sortira du résultat de la période.",
                "Supprimer",
                "Annuler");
            if (!confirmed)
                return;

            IsBusy = true;
            try
            {
                await _campaigns.DeleteAsync(campaign.id);
                Campaigns.Remove(campaign);
                ShowEmptyState = Campaigns.Count == 0;
            }
            catch (ApiException e) when (CampaignRequestBuilder.DeleteMessageFor(e.ApiError.Code) is { } message)
            {
                await _dialogService.ShowInfoAsync(message, "Suppression impossible");
                if (e.ApiError.Code == "CAMPAIGN_NOT_FOUND")
                {
                    Campaigns.Remove(campaign);
                    ShowEmptyState = Campaigns.Count == 0;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                await _dialogService.ShowErrorAsync(e);
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
