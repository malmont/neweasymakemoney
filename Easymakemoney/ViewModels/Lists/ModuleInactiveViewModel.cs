using System.Net;
using Easymakemoney.Services.Api;
using Easymakemoney.Services.Dialog;
using Easymakemoney.Services.Module;

namespace Easymakemoney.ViewModels.Lists
{
    /// <summary>
    /// Lot MOD : « Application de gestion non activée ». Le site se connecte, mais ses routes de gestion répondent
    /// 403 MODULE_INACTIVE : le gérant demande l'essai (ouvert aussitôt si la politique le permet) ou l'activation.
    /// </summary>
    public partial class ModuleInactiveViewModel : BaseViewModel
    {
        private readonly IManagementModuleService _moduleService;
        private readonly IModuleInactiveNotifier _notifier;
        private readonly IDialogService _dialogService;

        public ModuleInactiveViewModel(IManagementModuleService moduleService, IModuleInactiveNotifier notifier, IDialogService dialogService)
        {
            _moduleService = moduleService;
            _notifier = notifier;
            _dialogService = dialogService;
            Info = notifier.Last ?? new ModuleInactiveInfo { Module = ModuleInactiveInfo.ManagementModule };
        }

        /// <summary>Appelé quand l'appli revient à l'accueil après l'essai ou l'accord.</summary>
        public Func<Task> GoHomeAsync { get; set; } = () => Shell.Current?.GoToAsync("//DashboardPage") ?? Task.CompletedTask;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Explanation), nameof(CanRequest), nameof(RequestButtonText))]
        private ModuleInactiveInfo _info;

        public string Explanation => Info.Explanation;
        public bool CanRequest => Info.CanRequest && !IsBusy;
        public string RequestButtonText => Info.RequestButtonText;

        protected override void OnPropertyChanged(System.ComponentModel.PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            if (e.PropertyName == nameof(IsBusy))
                OnPropertyChanged(nameof(CanRequest));
        }

        /// <summary>Relit le dernier refus (l'écran peut s'ouvrir avant la fin de la lecture du 403).</summary>
        public void Refresh()
        {
            if (_notifier.Last != null)
                Info = _notifier.Last;
        }

        [RelayCommand]
        private async Task Request()
        {
            if (!Info.CanRequest || IsBusy)
                return;

            IsBusy = true;
            try
            {
                var result = await _moduleService.RequestAsync(Info.Feature);
                await _dialogService.ShowInfoAsync(result.ConfirmationText, result.IsTrial ? "Essai ouvert" : "Demande envoyée");
                if (result.IsTrial)
                {
                    _notifier.Reset();
                    await GoHomeAsync();
                    return;
                }

                Info = Copy(Info, pending: true);
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.Conflict && ex.ApiError.Code == "ALREADY_ENABLED")
            {
                await _dialogService.ShowInfoAsync("L'application de gestion est déjà active sur ce site.", "Déjà active");
                _notifier.Reset();
                await GoHomeAsync();
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.Conflict && ex.ApiError.Code == "REQUEST_PENDING")
            {
                Info = Copy(Info, pending: true);
                await _dialogService.ShowInfoAsync("Une demande est déjà en attente : la plateforme vous répondra par courriel.", "Demande en attente");
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

        /// <summary>Retour à l'accueil : si le module a été accordé entre-temps, tout se recharge normalement.</summary>
        [RelayCommand]
        private async Task Retry()
        {
            _notifier.Reset();
            await GoHomeAsync();
        }

        private static ModuleInactiveInfo Copy(ModuleInactiveInfo info, bool pending) => new()
        {
            Module = info.Module,
            Feature = info.Feature,
            Message = info.Message,
            Requestable = info.Requestable,
            Pending = pending,
            TrialDays = info.TrialDays,
        };
    }
}
