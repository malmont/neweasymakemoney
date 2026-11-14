using Easymakemoney.Services.Api;
using Easymakemoney.Services.Dialog;
using Easymakemoney.ViewModels.Statistiques;

namespace Easymakemoney.ViewModels.Lists
{
    /// <summary>
    /// Prévention des pertes, sur le téléphone : la synthèse et la liste des constats, pour suivre et décider au quotidien.
    /// Règles, seuils, réglages et notice aux salariés se règlent sur le web.
    /// </summary>
    public partial class LossFindingListViewModel : BaseViewModel
    {
        public const int PageSize = 20;
        public const string FilterNew = "new";
        public const string FilterActive = "open,watch";
        public const string FilterAll = "";

        private readonly ILossPreventionService _service;
        private readonly IDialogService _dialogService;
        private int _page = 1;
        private bool _loaded;

        public LossFindingListViewModel(ILossPreventionService service, IDialogService dialogService)
        {
            _service = service;
            _dialogService = dialogService;
            Filters = new ObservableCollection<StatsChoice>(new[]
            {
                new StatsChoice(FilterActive, "À traiter") { IsSelected = true },
                new StatsChoice(FilterNew, "Nouveaux"),
                new StatsChoice("explained,dismissed,confirmed", "Décidés"),
                new StatsChoice(FilterAll, "Tous"),
            });
        }

        /// <summary>Vrai après une décision : la liste et la synthèse se relisent au retour.</summary>
        public static bool FindingsChanged { get; set; }

        public Func<string, Task> NavigateAsync { get; set; } = route => Shell.Current?.GoToAsync(route) ?? Task.CompletedTask;

        public ObservableCollection<StatsChoice> Filters { get; }
        public ObservableCollection<LossFinding> Findings { get; } = new();

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasSummary))]
        private LossSummary? _summary;

        public bool HasSummary => Summary != null;

        [ObservableProperty]
        private bool _hasMore;

        [ObservableProperty]
        private bool _isEmpty;

        /// <summary>Module non accordé (403 MODULE_INACTIVE, module loss_prevention) : la gestion, elle, reste ouverte.</summary>
        [ObservableProperty]
        private bool _isModuleInactive;

        [ObservableProperty]
        private string _moduleInactiveText = string.Empty;

        private string SelectedFilter => Filters.FirstOrDefault(filter => filter.IsSelected)?.Code ?? FilterActive;

        /// <summary>Ouverture filtrée (vignette « nouveaux constats », lien pertes.constats avec isNew).</summary>
        public void UseInitialFilter(string? code)
        {
            if (code is null || Filters.All(filter => filter.Code != code))
                return;
            foreach (var filter in Filters)
                filter.IsSelected = filter.Code == code;
        }

        public async Task InitializeAsync()
        {
            if (_loaded && !FindingsChanged)
                return;
            FindingsChanged = false;
            await LoadSummaryAsync();
            if (!IsModuleInactive)
                await ReloadAsync();
            _loaded = !IsModuleInactive;
        }

        private async Task LoadSummaryAsync()
        {
            try
            {
                Summary = await _service.GetSummaryAsync();
                IsModuleInactive = false;
            }
            catch (ApiException ex) when (ex.ApiError.Code == ModuleInactiveInfo.Code)
            {
                Summary = null;
                IsModuleInactive = true;
                ModuleInactiveText = "Ce site n'a pas encore la Prévention des pertes. Elle s'essaie gratuitement ou se demande sur le web, dans la gestion, thème « Prévention des pertes ».";
                Findings.Clear();
                IsEmpty = false;
                HasMore = false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                await _dialogService.ShowErrorAsync(ex);
            }
        }

        public Task ReloadAsync()
        {
            _page = 1;
            return LoadAsync(append: false);
        }

        private async Task LoadAsync(bool append)
        {
            if (IsBusy)
                return;
            IsBusy = true;
            try
            {
                var filter = SelectedFilter;
                var onlyNew = filter == FilterNew;
                var list = await _service.GetFindingsAsync(onlyNew ? null : filter, onlyNew, _page, PageSize);
                if (!append)
                    Findings.Clear();
                foreach (var finding in list)
                    Findings.Add(finding);
                HasMore = list.Count == PageSize;
                IsEmpty = Findings.Count == 0;
            }
            catch (ApiException ex) when (ex.ApiError.Code == ModuleInactiveInfo.Code)
            {
                Findings.Clear();
                IsModuleInactive = true;
                HasMore = false;
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
        private async Task SelectFilter(string code)
        {
            foreach (var filter in Filters)
                filter.IsSelected = filter.Code == code;
            await ReloadAsync();
        }

        [RelayCommand]
        private async Task LoadMore()
        {
            if (!HasMore || IsBusy)
                return;
            _page++;
            await LoadAsync(append: true);
        }

        [RelayCommand]
        private async Task Refresh()
        {
            await LoadSummaryAsync();
            if (!IsModuleInactive)
                await ReloadAsync();
        }

        [RelayCommand]
        private Task Open(LossFinding? finding) =>
            finding == null ? Task.CompletedTask : NavigateAsync($"LossFindingPage?FindingId={finding.id}");
    }

    /// <summary>
    /// La fiche d'un constat : résumé, preuves dans l'ordre du temps, qui était en poste (« ce n'est pas une accusation »),
    /// et la décision du gérant, toujours la sienne. Une note est demandée pour « Expliqué » et « Confirmé ».
    /// </summary>
    public partial class LossFindingViewModel : BaseViewModel
    {
        public static readonly IReadOnlyList<(string Code, string Label)> Decisions = new[]
        {
            ("watch", "À suivre"),
            ("explained", "Expliqué"),
            ("dismissed", "Classé sans suite"),
            ("confirmed", "Confirmé"),
            ("open", "Revenir à « Nouveau »"),
        };

        private readonly ILossPreventionService _service;
        private readonly IDialogService _dialogService;

        public LossFindingViewModel(ILossPreventionService service, IDialogService dialogService)
        {
            _service = service;
            _dialogService = dialogService;
            Choices = new ObservableCollection<StatsChoice>(Decisions.Select(d => new StatsChoice(d.Code, d.Label)));
        }

        public int FindingId { get; set; }

        public ObservableCollection<StatsChoice> Choices { get; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasFinding))]
        private LossFindingDetail? _finding;

        public bool HasFinding => Finding != null;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(NoteHint), nameof(CanDecide))]
        private string? _selectedDecision;

        [ObservableProperty]
        private string _note = string.Empty;

        public static bool NoteRequired(string? status) => status is "explained" or "confirmed";

        public string NoteHint => NoteRequired(SelectedDecision)
            ? "Une note est demandée : dites ce que vous avez vérifié."
            : "Note facultative.";

        public bool CanDecide => SelectedDecision != null && SelectedDecision != Finding?.status;

        public async Task LoadAsync()
        {
            if (FindingId <= 0 || IsBusy)
                return;
            IsBusy = true;
            try
            {
                Finding = await _service.GetFindingAsync(FindingId);
                SelectDecision(null);
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
        private void SelectDecision(string? code)
        {
            SelectedDecision = code;
            foreach (var choice in Choices)
                choice.IsSelected = choice.Code == code;
        }

        [RelayCommand]
        private async Task Decide()
        {
            if (Finding == null || SelectedDecision is not { } status || IsBusy)
                return;
            if (NoteRequired(status) && string.IsNullOrWhiteSpace(Note))
            {
                await _dialogService.ShowInfoAsync("Une note est demandée pour « Expliqué » et « Confirmé » : dites ce que vous avez vérifié.");
                return;
            }
            IsBusy = true;
            try
            {
                Finding = await _service.DecideAsync(Finding.id, status, Note);
                Note = string.Empty;
                SelectDecision(null);
                LossFindingListViewModel.FindingsChanged = true;
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
    }
}
