using Easymakemoney.Services.Dialog;
using Easymakemoney.Utilities;

namespace Easymakemoney.ViewModels.Lists
{
    /// <summary>
    /// Guide de la période : conseils chiffrés par le backend, rédigés par l'IA quand elle est disponible.
    /// Rien ne s'applique tout seul : chaque conseil ouvre l'écran où le gérant décide.
    /// </summary>
    public partial class AdviceViewModel : BaseViewModel
    {
        private const int MaxPolls = 6;

        private readonly IAdviceService _adviceService;
        private readonly IDialogService _dialogService;
        private int _polls;
        private bool _pollScheduled;

        public AdviceViewModel(IAdviceService adviceService, IDialogService dialogService)
        {
            _adviceService = adviceService;
            _dialogService = dialogService;
        }

        /// <summary>Conseil mis en tête (ouvert depuis une vignette).</summary>
        public int FocusAdviceId { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasList))]
        private AdviceList? _list;

        public bool HasList => List != null;

        public ObservableCollection<Advice> Advices { get; } = new();

        /// <summary>Conseils appliqués, avec leur mesure (« marge +35,00 $ en 14 jours »).</summary>
        public ObservableCollection<Advice> DoneAdvices { get; } = new();

        [ObservableProperty]
        private bool _isEmpty;

        [ObservableProperty]
        private bool _hasDone;

        public async Task LoadAsync()
        {
            if (IsBusy)
                return;

            IsBusy = true;
            try
            {
                var list = await _adviceService.GetAsync();
                List = list;
                Advices.Clear();
                var ordered = list.advices
                    .Where(advice => advice.IsNew)
                    .OrderByDescending(advice => advice.id == FocusAdviceId)
                    .ThenBy(advice => advice.rank);
                foreach (var advice in ordered)
                    Advices.Add(advice);
                IsEmpty = Advices.Count == 0;

                DoneAdvices.Clear();
                foreach (var advice in list.advices.Where(advice => advice.IsDone)
                             .OrderByDescending(advice => advice.id == FocusAdviceId)
                             .ThenByDescending(advice => advice.appliedOn))
                    DoneAdvices.Add(advice);
                HasDone = DoneAdvices.Count > 0;

                if (list.generating)
                    SchedulePoll();
                else
                    _polls = 0;
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

        /// <summary>La rédaction par l'IA tourne en tâche de fond : on relit la liste quelques fois, sans insister.</summary>
        private void SchedulePoll()
        {
            if (_pollScheduled || _polls >= MaxPolls)
                return;

            _pollScheduled = true;
            _polls++;
            _ = Task.Delay(TimeSpan.FromSeconds(6)).ContinueWith(_ => MainThread.BeginInvokeOnMainThread(async () =>
            {
                _pollScheduled = false;
                await LoadAsync();
            }), TaskScheduler.Default);
        }

        [RelayCommand]
        private async Task Open(Advice? advice)
        {
            if (advice?.action == null)
                return;

            await ScreenNavigator.OpenAsync(advice.action.screen, advice.action.@params, advice.figures, advice.title, advice.text, advice.Currency);
        }

        [RelayCommand]
        private async Task Done(Advice? advice)
        {
            if (advice == null)
                return;

            // Hausse de prix ou promotion : le gérant précise le pourcentage réellement appliqué, pour que l'effet soit mesuré.
            Dictionary<string, object>? applied = null;
            if (advice.AppliedPercentField is { } field && Application.Current?.Windows.FirstOrDefault()?.Page is { } page)
            {
                var question = field == "pricePercent" ? "Hausse de prix appliquée (en %)" : "Promotion appliquée (en %)";
                var initial = advice.SuggestedPercent is { } suggested ? suggested.ToString("0.##") : string.Empty;
                var text = await page.DisplayPromptAsync(
                    "Conseil appliqué",
                    $"{question}. Le guide mesurera l'effet sur 14 jours à partir d'aujourd'hui.",
                    "C'est fait", "Annuler", initialValue: initial, keyboard: Keyboard.Numeric);
                if (text == null)
                    return;

                if (Converters.DecimalInputConverter.Parse(text) is { } percent && percent != 0)
                    applied = new Dictionary<string, object> { [field] = percent };
            }
            else
            {
                var confirmed = await _dialogService.ConfirmAsync(
                    "Conseil appliqué",
                    "Marquer ce conseil comme fait ? Il ne reviendra plus pour cette période.",
                    "C'est fait", "Annuler");
                if (!confirmed)
                    return;
            }

            try
            {
                await _adviceService.DoneAsync(advice.id, applied: applied);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                await _dialogService.ShowErrorAsync(ex);
                return;
            }

            // Recharge : le conseil passe dans « Conseils appliqués », où sa mesure apparaîtra.
            await LoadAsync();
        }

        /// <summary>Applique la hausse conseillée : aperçu puis confirmation ; le conseil est marqué fait par le backend.</summary>
        [RelayCommand]
        private async Task ChangePrice(Advice? advice)
        {
            if (advice is not { CanChangePrice: true } || Shell.Current is not { } shell)
                return;

            var percent = advice.SuggestedPercent is { } value
                ? $"&Percent={value.ToString(System.Globalization.CultureInfo.InvariantCulture)}"
                : string.Empty;
            await shell.GoToAsync($"{nameof(PriceChangePage)}?AdviceId={advice.id}{percent}");
        }

        /// <summary>Crée la promotion conseillée : aperçu, dates, puis confirmation ; le conseil est marqué fait par le backend.</summary>
        [RelayCommand]
        private async Task CreatePromotion(Advice? advice)
        {
            if (advice is not { CanCreatePromotion: true } || Shell.Current is not { } shell)
                return;

            var percent = advice.SuggestedPercent is { } value
                ? $"&Percent={value.ToString(System.Globalization.CultureInfo.InvariantCulture)}"
                : string.Empty;
            await shell.GoToAsync($"{nameof(PromotionCreatePage)}?AdviceId={advice.id}{percent}");
        }

        [RelayCommand]
        private async Task Dismiss(Advice? advice)
        {
            if (advice == null)
                return;

            await CloseAsync(advice, () => _adviceService.DismissAsync(advice.id));
        }

        private async Task CloseAsync(Advice advice, Func<Task> action)
        {
            try
            {
                await action();
                Advices.Remove(advice);
                IsEmpty = Advices.Count == 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                await _dialogService.ShowErrorAsync(ex);
            }
        }

        [RelayCommand]
        private async Task Refresh()
        {
            if (List is not { CanRefresh: true } || IsBusy)
                return;

            try
            {
                await _adviceService.RefreshAsync(List.period?.id);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                await _dialogService.ShowErrorAsync(ex);
                return;
            }

            _polls = 0;
            await LoadAsync();
        }
    }
}
