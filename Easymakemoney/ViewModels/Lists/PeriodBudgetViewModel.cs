using Easymakemoney.Converters;
using Easymakemoney.Services.Dialog;
using Easymakemoney.Utilities;

namespace Easymakemoney.ViewModels.Lists
{
    /// <summary>Budget par poste d'une période : prévu contre réel, poste par poste (lot backend B1a).</summary>
    public partial class PeriodBudgetViewModel : BaseViewModel
    {
        private readonly IPeriodBudgetService _budgetService;
        private readonly IDialogService _dialogService;

        public PeriodBudgetViewModel(IPeriodBudgetService budgetService, IDialogService dialogService)
        {
            _budgetService = budgetService;
            _dialogService = dialogService;
        }

        public int PeriodId { get; set; }

        /// <summary>Période fermée : lecture seule, comme partout ailleurs.</summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanEdit))]
        private bool _readOnly;

        public bool CanEdit => !ReadOnly && Budget != null;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanEdit))]
        [NotifyPropertyChangedFor(nameof(HasBudget))]
        private PeriodBudget? _budget;

        public bool HasBudget => Budget != null;

        public ObservableCollection<PeriodBudgetLine> Lines { get; } = new();

        [ObservableProperty]
        private string? _periodName;

        public async Task LoadAsync()
        {
            if (PeriodId <= 0 || IsBusy)
                return;

            IsBusy = true;
            try
            {
                Show(await _budgetService.GetAsync(PeriodId));
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
        private async Task EditLine(PeriodBudgetLine? line)
        {
            var page = Application.Current?.MainPage;
            if (line?.category == null || page == null || !CanEdit || IsBusy)
                return;

            var initial = line.HasPlanned ? (line.planned / 100.0).ToString("0.##") : string.Empty;
            var text = await page.DisplayPromptAsync(
                $"Prévu : {line.label}",
                "Montant prévu pour ce poste. Laissez 0 pour l'effacer.",
                "Valider", "Annuler", initialValue: initial, keyboard: Keyboard.Numeric);
            if (text == null)
                return;

            var amount = string.IsNullOrWhiteSpace(text) ? 0 : DecimalInputConverter.Parse(text);
            if (amount is null or < 0)
            {
                await _dialogService.ShowInfoAsync("Montant invalide.");
                return;
            }

            await SaveAsync(() => _budgetService.SavePlannedAsync(PeriodId, line.category, MoneyHelper.ToCents(amount)));
        }

        [RelayCommand]
        private async Task CopyLastYear()
        {
            if (!CanEdit || IsBusy)
                return;

            var confirmed = await _dialogService.ConfirmAsync(
                "Reprendre l'an dernier",
                "Le prévu de chaque poste sera remplacé par celui de la même période l'an dernier.",
                "Reprendre", "Annuler");
            if (!confirmed)
                return;

            await SaveAsync(() => _budgetService.CopyAsync(PeriodId));
        }

        private async Task SaveAsync(Func<Task<PeriodBudget>> action)
        {
            IsBusy = true;
            try
            {
                Show(await action());
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

        private void Show(PeriodBudget budget)
        {
            Budget = budget;
            PeriodName = budget.period?.name;
            Lines.Clear();
            foreach (var line in budget.lines)
                Lines.Add(line);
        }
    }
}
