using Easymakemoney.Services.Dialog;
using Easymakemoney.Utilities;

namespace Easymakemoney.ViewModels.Lists
{
    /// <summary>
    /// Frais fixes : les sorties récurrentes (loyer, salaires, assurances, abonnements…) saisies dans la trésorerie,
    /// avec leur équivalent mensuel et le total par mois. La saisie réutilise le formulaire des mouvements prévus.
    /// </summary>
    public partial class FixedCostsViewModel : BaseViewModel
    {
        private readonly ITreasuryService _treasuryService;
        private readonly IDialogService _dialogService;

        public FixedCostsViewModel(ITreasuryService treasuryService, IDialogService dialogService)
        {
            _treasuryService = treasuryService;
            _dialogService = dialogService;
        }

        public ObservableCollection<TreasuryMovement> Items { get; } = new();

        [ObservableProperty]
        private string _monthlyTotalText = string.Empty;

        [ObservableProperty]
        private bool _isEmpty;

        public async Task LoadAsync()
        {
            if (IsBusy)
                return;

            IsBusy = true;
            try
            {
                var movements = await _treasuryService.GetMovementsAsync();
                var fixedCosts = FixedCosts.Select(movements).ToList();
                Items.Clear();
                foreach (var item in fixedCosts)
                    Items.Add(item);
                IsEmpty = Items.Count == 0;
                MonthlyTotalText = SiteCurrency.FormatCents(FixedCosts.MonthlyTotal(fixedCosts));
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
        private async Task Add()
        {
            if (Shell.Current is { } shell)
                await shell.GoToAsync(nameof(TreasuryMovementPage));
        }

        [RelayCommand]
        private async Task Delete(TreasuryMovement? movement)
        {
            if (movement?.id is not { } id || !movement.CanDelete)
                return;

            var confirmed = await _dialogService.ConfirmAsync("Supprimer ce frais fixe", $"Supprimer « {movement.label} » et ses prochaines échéances ?", "Supprimer", "Annuler");
            if (!confirmed)
                return;

            try
            {
                await _treasuryService.DeleteMovementAsync(id);
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
