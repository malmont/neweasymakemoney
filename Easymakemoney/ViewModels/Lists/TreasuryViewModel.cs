using Easymakemoney.Converters;
using Easymakemoney.Services.Dialog;
using Easymakemoney.Utilities;

namespace Easymakemoney.ViewModels.Lists
{
    /// <summary>Trésorerie saisie : solde du compte, projection sur 90 jours, mouvements prévus (lot backend B1b-1).</summary>
    public partial class TreasuryViewModel : BaseViewModel
    {
        private readonly ITreasuryService _treasuryService;
        private readonly IDialogService _dialogService;
        private bool _applyingSettings;

        public TreasuryViewModel(ITreasuryService treasuryService, IDialogService dialogService)
        {
            _treasuryService = treasuryService;
            _dialogService = dialogService;
        }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasOverview))]
        private TreasuryOverview? _overview;

        public bool HasOverview => Overview != null;

        public ObservableCollection<TreasuryDay> Upcoming { get; } = new();
        public ObservableCollection<TreasuryMovement> Movements { get; } = new();
        public ObservableCollection<TreasuryBalance> Balances { get; } = new();

        [ObservableProperty]
        private bool _hasUpcoming;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasNoMovements))]
        private bool _hasMovements;

        public bool HasNoMovements => !HasMovements;

        /// <summary>Réglage « compter les ventes estimées » ; le changer l'enregistre aussitôt.</summary>
        [ObservableProperty]
        private bool _includeEstimatedSales;

        // Saisie d'un solde
        [ObservableProperty]
        private DateTime _balanceDate = DateTime.Today;

        public DateTime MaxBalanceDate => DateTime.Today;

        [ObservableProperty]
        private string? _balanceAmountText;

        [ObservableProperty]
        private bool _balanceIsOverdraft;

        [ObservableProperty]
        private string? _balanceNote;

        public async Task LoadAsync()
        {
            if (IsBusy)
                return;

            IsBusy = true;
            try
            {
                var overviewTask = _treasuryService.GetOverviewAsync();
                var movementsTask = _treasuryService.GetMovementsAsync();
                var balancesTask = _treasuryService.GetBalancesAsync(5);
                await Task.WhenAll(overviewTask, movementsTask, balancesTask);

                var overview = overviewTask.Result;
                Overview = overview;

                _applyingSettings = true;
                IncludeEstimatedSales = overview.settings.includeEstimatedSales;
                _applyingSettings = false;

                Upcoming.Clear();
                foreach (var day in overview.UpcomingDays(DateTime.Today))
                    Upcoming.Add(day);
                HasUpcoming = Upcoming.Count > 0;

                Movements.Clear();
                foreach (var movement in movementsTask.Result)
                {
                    movement.Currency = overview.currency;
                    Movements.Add(movement);
                }
                HasMovements = Movements.Count > 0;

                Balances.Clear();
                foreach (var balance in balancesTask.Result)
                {
                    balance.Currency = overview.currency;
                    Balances.Add(balance);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                await _dialogService.ShowErrorAsync(ex);
            }
            finally
            {
                _applyingSettings = false;
                IsBusy = false;
            }
        }

        partial void OnIncludeEstimatedSalesChanged(bool value)
        {
            if (_applyingSettings || Overview == null)
                return;
            _ = SaveSettingsAsync(null, value);
        }

        [RelayCommand]
        private async Task AddBalance()
        {
            if (IsBusy)
                return;

            if (DecimalInputConverter.Parse(BalanceAmountText) is not { } amount || amount < 0)
            {
                await _dialogService.ShowInfoAsync("Saisissez le solde du compte (par exemple 2 450,30). Cochez « Découvert » s'il est négatif.");
                return;
            }

            var cents = MoneyHelper.ToCents(amount) * (BalanceIsOverdraft ? -1 : 1);
            try
            {
                await _treasuryService.AddBalanceAsync(BalanceDate, cents, BalanceNote);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                await _dialogService.ShowErrorAsync(ex);
                return;
            }

            BalanceAmountText = null;
            BalanceNote = null;
            BalanceIsOverdraft = false;
            await LoadAsync();
        }

        [RelayCommand]
        private async Task DeleteBalance(TreasuryBalance? balance)
        {
            if (balance == null || IsBusy)
                return;

            var confirmed = await _dialogService.ConfirmAsync("Supprimer ce solde", $"Supprimer le solde du {balance.DateText} ?", "Supprimer", "Annuler");
            if (!confirmed)
                return;

            await RunAndReloadAsync(() => _treasuryService.DeleteBalanceAsync(balance.id));
        }

        [RelayCommand]
        private async Task AddMovement()
        {
            if (Shell.Current is { } shell)
                await shell.GoToAsync(nameof(TreasuryMovementPage));
        }

        [RelayCommand]
        private async Task DeleteMovement(TreasuryMovement? movement)
        {
            if (movement?.id is not { } id || !movement.CanDelete || IsBusy)
                return;

            var confirmed = await _dialogService.ConfirmAsync("Supprimer ce mouvement", $"Supprimer « {movement.label} » et ses prochaines échéances ?", "Supprimer", "Annuler");
            if (!confirmed)
                return;

            await RunAndReloadAsync(() => _treasuryService.DeleteMovementAsync(id));
        }

        [RelayCommand]
        private async Task EditThreshold()
        {
            var page = Application.Current?.Windows.FirstOrDefault()?.Page;
            if (page == null || Overview == null || IsBusy)
                return;

            var initial = (Overview.settings.threshold / 100.0).ToString("0.##");
            var text = await page.DisplayPromptAsync(
                "Seuil d'alerte",
                "Vous êtes prévenu quand le solde prévu passe sous ce montant dans les 14 prochains jours. 0 : seulement sous zéro.",
                "Valider", "Annuler", initialValue: initial, keyboard: Keyboard.Numeric);
            if (text == null)
                return;

            var amount = string.IsNullOrWhiteSpace(text) ? 0 : DecimalInputConverter.Parse(text);
            if (amount is null or < 0)
            {
                await _dialogService.ShowInfoAsync("Montant invalide.");
                return;
            }

            await SaveSettingsAsync(MoneyHelper.ToCents(amount), null);
        }

        private async Task SaveSettingsAsync(long? threshold, bool? includeEstimatedSales)
        {
            await RunAndReloadAsync(() => _treasuryService.UpdateSettingsAsync(threshold, includeEstimatedSales));
        }

        private async Task RunAndReloadAsync(Func<Task> action)
        {
            try
            {
                await action();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                await _dialogService.ShowErrorAsync(ex);
            }

            await LoadAsync();
        }
    }
}
