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

        [ObservableProperty]
        private bool _hasBalances;

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
                var reconciliationsTask = _treasuryService.GetReconciliationsAsync(5);
                await Task.WhenAll(overviewTask, movementsTask, balancesTask, reconciliationsTask);

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
                HasBalances = Balances.Count > 0;

                Reconciliations.Clear();
                foreach (var reconciliation in reconciliationsTask.Result.items)
                    Reconciliations.Add(reconciliation);
                HasReconciliations = Reconciliations.Count > 0;
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
            Reconciliation? reconciliation;
            try
            {
                reconciliation = await _treasuryService.AddBalanceAsync(BalanceDate, cents, BalanceNote);
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

            // Rapprochement : l'application compare le solde saisi à ce qu'elle attendait et propose de traiter l'écart.
            if (reconciliation is { IsMatched: true })
                await _dialogService.ShowInfoAsync("Le solde correspond à ce que l'application attendait. Rien à faire.", "Rapprochement");
            else if (reconciliation is { IsOpen: true })
            {
                reconciliation.Currency = Overview?.currency;
                await HandleGap(reconciliation);
            }
        }

        public ObservableCollection<Reconciliation> Reconciliations { get; } = new();

        [ObservableProperty]
        private bool _hasReconciliations;

        /// <summary>
        /// Écart entre le solde saisi et l'attendu : retrouver la dépense oubliée (reçu, note de frais), marquer l'écart
        /// « non justifié », ou l'expliquer (par exemple un dépôt d'espèces). L'écart reste vivant : une dépense saisie plus tard le réduit.
        /// </summary>
        [RelayCommand]
        private async Task HandleGap(Reconciliation? reconciliation)
        {
            if (reconciliation == null || reconciliation.IsMatched || Shell.Current is not { } shell)
                return;

            const string readReceipt = "Lire un reçu oublié";
            const string addExpense = "Ajouter une note de frais";
            const string unexplained = "Marquer « écart non justifié »";
            const string explain = "Expliquer l'écart (dépôt d'espèces…)";
            const string reopen = "Revenir à « à traiter »";

            var choices = new List<string> { readReceipt, addExpense };
            if (reconciliation.status != "unexplained") choices.Add(unexplained);
            if (reconciliation.status != "explained") choices.Add(explain);
            if (!reconciliation.IsOpen) choices.Add(reopen);

            var details = $"{reconciliation.CompositionText}\n\n{reconciliation.WindowText}";
            var choice = await _dialogService.ChooseAsync(reconciliation.GapText, choices, "Plus tard", details);

            switch (choice)
            {
                case readReceipt:
                    await shell.GoToAsync($"{nameof(DocumentReadPage)}?Type=note");
                    break;
                case addExpense:
                    await shell.GoToAsync(nameof(ListNewNoteDeFraisPage));
                    break;
                case unexplained:
                    await DecideAsync(reconciliation, "unexplained", null);
                    break;
                case explain:
                    var note = await _dialogService.PromptAsync("Expliquer l'écart", "Par exemple : dépôt d'espèces de la caisse le 08/11.");
                    if (!string.IsNullOrWhiteSpace(note))
                        await DecideAsync(reconciliation, "explained", note);
                    break;
                case reopen:
                    await DecideAsync(reconciliation, "open", null);
                    break;
            }
        }

        private async Task DecideAsync(Reconciliation reconciliation, string decision, string? note)
        {
            try
            {
                await _treasuryService.DecideGapAsync(reconciliation.balanceId, decision, note);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                await _dialogService.ShowErrorAsync(ex);
            }

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

        /// <summary>Corriger un solde déjà saisi : montant (un « - » pour un découvert), puis la note.</summary>
        [RelayCommand]
        private async Task EditBalance(TreasuryBalance? balance)
        {
            if (balance == null || IsBusy)
                return;

            var initial = (balance.amount / 100.0).ToString("0.00", System.Globalization.CultureInfo.CurrentCulture);
            var text = await _dialogService.PromptAsync(
                $"Solde du {balance.DateText}",
                "Le solde exact du compte ce jour-là. Mettez un « - » devant pour un découvert.",
                "Suivant", "Annuler", initialValue: initial, keyboard: Keyboard.Text);
            if (text == null)
                return;
            if (ParseSignedAmount(text) is not { } amount)
            {
                await _dialogService.ShowInfoAsync("Montant invalide. Exemple : 2 450,30 ou -150,00.");
                return;
            }

            var note = await _dialogService.PromptAsync("Note (facultatif)", "Par exemple : relevé du soir.", "Enregistrer", "Annuler",
                initialValue: balance.note ?? string.Empty);
            if (note == null)
                return;

            await RunAndReloadAsync(() => _treasuryService.UpdateBalanceAsync(balance.id, MoneyHelper.ToCents(amount), note));
        }

        /// <summary>Montant avec signe (« -150,00 », « −150 ») : le convertisseur commun refuse les signes.</summary>
        public static double? ParseSignedAmount(string? text)
        {
            var trimmed = text?.Trim() ?? string.Empty;
            var negative = trimmed.StartsWith('-') || trimmed.StartsWith('−');
            if (negative || trimmed.StartsWith('+'))
                trimmed = trimmed[1..];
            return DecimalInputConverter.Parse(trimmed) is { } value ? (negative ? -value : value) : null;
        }

        [RelayCommand]
        private async Task EditTolerance()
        {
            if (Overview == null || IsBusy)
                return;

            var initial = (Overview.Tolerance / 100.0).ToString("0.##", System.Globalization.CultureInfo.CurrentCulture);
            var text = await _dialogService.PromptAsync(
                "Écart toléré",
                "Un écart plus petit que ce montant entre le solde saisi et l'attendu est considéré comme rapproché (arrondis, petits frais).",
                "Valider", "Annuler", initialValue: initial, keyboard: Keyboard.Numeric);
            if (text == null)
                return;

            var amount = string.IsNullOrWhiteSpace(text) ? 0 : DecimalInputConverter.Parse(text);
            if (amount is null or < 0)
            {
                await _dialogService.ShowInfoAsync("Montant invalide.");
                return;
            }

            await RunAndReloadAsync(() => _treasuryService.UpdateSettingsAsync(null, null, MoneyHelper.ToCents(amount)));
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
            if (Overview == null || IsBusy)
                return;

            var initial = (Overview.settings.threshold / 100.0).ToString("0.##");
            var text = await _dialogService.PromptAsync(
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
