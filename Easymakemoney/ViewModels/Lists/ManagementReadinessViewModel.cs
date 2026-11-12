using Easymakemoney.Services.Dialog;

namespace Easymakemoney.ViewModels.Lists
{
    /// <summary>
    /// Lot CHE : « Préparer ma gestion ». Étapes du serveur (groupe app.management de /api/site/readiness) :
    /// d'abord ce qui bloque, puis ce qui est conseillé, puis ce qui est fait. Chaque étape mène à son écran,
    /// ou dit qu'elle se fait sur le site.
    /// </summary>
    public partial class ManagementReadinessViewModel : BaseViewModel
    {
        private readonly IManagementModuleService _moduleService;
        private readonly IDialogService _dialogService;

        public ManagementReadinessViewModel(IManagementModuleService moduleService, IDialogService dialogService)
        {
            _moduleService = moduleService;
            _dialogService = dialogService;
        }

        /// <summary>Navigation vers une page de l'appli (remplaçable dans les tests).</summary>
        public Func<string, Task> NavigateAsync { get; set; } = route => Shell.Current?.GoToAsync(route) ?? Task.CompletedTask;

        public ObservableCollection<ReadinessStep> Blocking { get; } = new();
        public ObservableCollection<ReadinessStep> Improvements { get; } = new();
        public ObservableCollection<ReadinessStep> Done { get; } = new();

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HeadlineText), nameof(ProgressText), nameof(Percent), nameof(HasBlocking), nameof(HasImprovements), nameof(HasDone), nameof(IsLoaded))]
        private ManagementReadinessSummary? _summary;

        public bool IsLoaded => Summary != null;
        public string HeadlineText => Summary?.HeadlineText ?? string.Empty;
        public string ProgressText => Summary?.ProgressText ?? string.Empty;
        public double Percent => (Summary?.Percent ?? 0) / 100.0;
        public bool HasBlocking => Blocking.Count > 0;
        public bool HasImprovements => Improvements.Count > 0;
        public bool HasDone => Done.Count > 0;

        public async Task LoadAsync()
        {
            if (IsBusy)
                return;

            IsBusy = true;
            try
            {
                var readiness = await _moduleService.GetReadinessAsync();
                var summary = new ManagementReadinessSummary(readiness.ManagementSteps);
                Fill(Blocking, summary.Blocking);
                Fill(Improvements, summary.Improvements);
                Fill(Done, summary.Done);
                Summary = summary;
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

        [RelayCommand]
        private async Task Open(ReadinessStep? step)
        {
            if (step == null || step.IsDone)
                return;

            if (step.Route is { } route)
            {
                await NavigateAsync(route);
                return;
            }

            await _dialogService.ShowInfoAsync(
                step.appScreen == "notifications"
                    ? "Les notifications arrivent bientôt dans l'application."
                    : "Cette étape se fait sur le site, dans l'administration : Application de gestion › Réglages et cheminement.",
                step.Title);
        }

        private static void Fill(ObservableCollection<ReadinessStep> target, IEnumerable<ReadinessStep> steps)
        {
            target.Clear();
            foreach (var step in steps)
                target.Add(step);
        }
    }
}
