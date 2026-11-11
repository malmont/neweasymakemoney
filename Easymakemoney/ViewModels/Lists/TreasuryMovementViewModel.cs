using Easymakemoney.Converters;
using Easymakemoney.Services.Dialog;
using Easymakemoney.Utilities;

namespace Easymakemoney.ViewModels.Lists
{
    /// <summary>Nouveau mouvement prévu : loyer, salaires, échéance de prêt, taxes…</summary>
    public partial class TreasuryMovementViewModel : BaseViewModel
    {
        private readonly ITreasuryService _treasuryService;
        private readonly IDialogService _dialogService;

        public TreasuryMovementViewModel(ITreasuryService treasuryService, IDialogService dialogService)
        {
            _treasuryService = treasuryService;
            _dialogService = dialogService;
            SelectedCategory = Categories[0];
            SelectedDirection = Directions[0];
            SelectedRecurrence = Recurrences.First(choice => choice.Code == "monthly");
        }

        public IReadOnlyList<TreasuryChoice> Categories => TreasuryChoices.Categories;
        public IReadOnlyList<TreasuryChoice> Directions => TreasuryChoices.Directions;
        public IReadOnlyList<TreasuryChoice> Recurrences => TreasuryChoices.Recurrences;

        [ObservableProperty]
        private string? _label;

        [ObservableProperty]
        private TreasuryChoice? _selectedCategory;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanBeFixedCost))]
        private TreasuryChoice? _selectedDirection;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsRecurring))]
        [NotifyPropertyChangedFor(nameof(CanBeFixedCost))]
        private TreasuryChoice? _selectedRecurrence;

        public bool IsRecurring => SelectedRecurrence?.Code is { } code && code != "none";

        /// <summary>Seule une sortie récurrente peut être un frais fixe (lot FF1).</summary>
        public bool CanBeFixedCost => FixedCostRule.CanApply(SelectedDirection?.Code, SelectedRecurrence?.Code);

        /// <summary>Suit la règle de la catégorie (loyer, salaires… oui ; prêt, taxes, autres non) tant que le gérant n'y a pas touché.</summary>
        [ObservableProperty]
        private bool _isFixedCost;

        private bool _fixedCostTouched;
        private bool _applyingDefault;

        partial void OnSelectedCategoryChanged(TreasuryChoice? value)
        {
            if (_fixedCostTouched)
                return;
            _applyingDefault = true;
            IsFixedCost = FixedCostRule.IsDefault(value?.Code);
            _applyingDefault = false;
        }

        partial void OnIsFixedCostChanged(bool value)
        {
            if (!_applyingDefault)
                _fixedCostTouched = true;
        }

        /// <summary>Le choix n'est envoyé que s'il diffère de la règle par défaut : sinon le backend applique sa règle.</summary>
        public bool? FixedCostChoice => CanBeFixedCost && IsFixedCost != FixedCostRule.IsDefault(SelectedCategory?.Code) ? IsFixedCost : null;

        [ObservableProperty]
        private string? _amountText;

        [ObservableProperty]
        private DateTime _startDate = DateTime.Today;

        [ObservableProperty]
        private bool _hasEndDate;

        [ObservableProperty]
        private DateTime _endDate = DateTime.Today.AddYears(1);

        [ObservableProperty]
        private string? _notes;

        [RelayCommand]
        private async Task Save()
        {
            if (IsBusy)
                return;

            if (string.IsNullOrWhiteSpace(Label))
            {
                await _dialogService.ShowInfoAsync("Donnez un nom au mouvement (par exemple « Loyer du local »).");
                return;
            }

            if (DecimalInputConverter.Parse(AmountText) is not { } amount || amount <= 0)
            {
                await _dialogService.ShowInfoAsync("Saisissez un montant supérieur à zéro.");
                return;
            }

            if (IsRecurring && HasEndDate && EndDate.Date < StartDate.Date)
            {
                await _dialogService.ShowInfoAsync("La date de fin doit suivre la date de début.");
                return;
            }

            var request = new TreasuryMovementRequest
            {
                label = Label.Trim(),
                category = SelectedCategory?.Code ?? "other",
                direction = SelectedDirection?.Code ?? "out",
                amount = MoneyHelper.ToCents(amount),
                startDate = TreasuryDates.Iso(StartDate),
                recurrence = SelectedRecurrence?.Code ?? "none",
                endDate = IsRecurring && HasEndDate ? TreasuryDates.Iso(EndDate) : null,
                notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim(),
                fixedCost = FixedCostChoice,
            };

            IsBusy = true;
            try
            {
                await _treasuryService.AddMovementAsync(request);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                await _dialogService.ShowErrorAsync(ex);
                return;
            }
            finally
            {
                IsBusy = false;
            }

            if (Shell.Current is { } shell)
                await shell.GoToAsync("..");
        }
    }
}
