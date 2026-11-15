using Easymakemoney.Services.Dialog;

namespace Easymakemoney.ViewModels.Lists
{
    /// <summary>
    /// Écran « Notifications » : le niveau des courriels de suivi de ce compte (« Comme le site » ou l'un des quatre niveaux),
    /// et, dans « Avancé », chaque type activable. Les courriels indispensables partent toujours.
    /// </summary>
    public partial class NotificationPreferencesViewModel : BaseViewModel
    {
        private readonly INotificationPreferencesService _service;
        private readonly IDialogService _dialogService;

        public NotificationPreferencesViewModel(INotificationPreferencesService service, IDialogService dialogService)
        {
            _service = service;
            _dialogService = dialogService;
        }

        public ObservableCollection<NotificationLevelChoice> Choices { get; } = new();
        public ObservableCollection<NotificationKind> Kinds { get; } = new();

        [ObservableProperty]
        private NotificationPreferences? _preferences;

        [ObservableProperty]
        private bool _showAdvanced;

        public Task LoadAsync() => RunAsync(() => _service.GetAsync());

        [RelayCommand]
        private Task SelectLevel(NotificationLevelChoice? choice)
        {
            if (choice == null || choice.IsSelected)
                return Task.CompletedTask;
            return RunAsync(() => _service.SetLevelAsync(choice.Key));
        }

        [RelayCommand]
        private Task ToggleKind(NotificationKind? kind)
        {
            if (kind?.kind is not { } code || !kind.available)
                return Task.CompletedTask;
            return RunAsync(() => _service.SetKindAsync(code, !kind.enabled));
        }

        [RelayCommand]
        private void ToggleAdvanced() => ShowAdvanced = !ShowAdvanced;

        private async Task RunAsync(Func<Task<NotificationPreferences>> call)
        {
            if (IsBusy)
                return;
            IsBusy = true;
            try
            {
                Show(await call());
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

        private void Show(NotificationPreferences preferences)
        {
            Preferences = preferences;
            Choices.Clear();
            foreach (var choice in preferences.Choices())
                Choices.Add(choice);
            Kinds.Clear();
            foreach (var kind in preferences.kinds.Where(k => k.available))
                Kinds.Add(kind);
        }
    }
}
