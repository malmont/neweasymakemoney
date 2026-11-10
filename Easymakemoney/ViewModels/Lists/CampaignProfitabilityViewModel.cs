using Easymakemoney.Services.Api;

namespace Easymakemoney.ViewModels.Lists
{
    /// <summary>Rentabilité perçue d'une campagne : gain en marge pour 1 $ investi, avant / pendant, remarques.</summary>
    public partial class CampaignProfitabilityViewModel : BaseViewModel
    {
        private readonly ICampaignService _campaigns;
        private int _campaignId;

        public CampaignProfitabilityViewModel(ICampaignService campaigns)
        {
            _campaigns = campaigns;
        }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasLoadError))]
        private string? loadError;

        public bool HasLoadError => !string.IsNullOrEmpty(LoadError);

        [ObservableProperty] private bool isLoaded;
        [ObservableProperty] private Campaign? campaign;
        [ObservableProperty] private CampaignProfitabilityReport? report;
        [ObservableProperty] private Microsoft.Maui.Graphics.Color gainColor = Microsoft.Maui.Graphics.Color.FromArgb(CampaignProfitabilityReport.NeutralColor);

        public async Task InitAsync(int campaignId)
        {
            _campaignId = campaignId;
            await LoadAsync();
        }

        [RelayCommand]
        private async Task Reload() => await LoadAsync();

        /// <summary>Retour de la modification : montant et dates ont pu changer.</summary>
        public Task RefreshAsync() => LoadAsync();

        private async Task LoadAsync()
        {
            if (IsBusy || _campaignId == 0)
                return;

            IsBusy = true;
            LoadError = null;
            try
            {
                var campaignTask = _campaigns.GetAsync(_campaignId);
                var profitabilityTask = _campaigns.GetProfitabilityAsync(_campaignId);
                await Task.WhenAll(campaignTask, profitabilityTask);

                Campaign = campaignTask.Result;
                var data = profitabilityTask.Result;
                if (data == null)
                {
                    LoadError = "Rentabilité indisponible pour le moment.";
                    return;
                }

                Report = new CampaignProfitabilityReport(data);
                GainColor = Microsoft.Maui.Graphics.Color.FromArgb(Report.GainColor);
                IsLoaded = true;
            }
            catch (ApiException e) when (e.ApiError.Code == "CAMPAIGN_NOT_FOUND")
            {
                LoadError = "Cette campagne n'existe plus.";
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                LoadError = "Impossible de calculer la rentabilité. Vérifiez la connexion et réessayez.";
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task Edit()
        {
            if (_campaignId > 0 && Shell.Current is { } shell)
                await shell.GoToAsync($"{nameof(CampaignFormPage)}?CampaignId={_campaignId}");
        }
    }
}
