using Easymakemoney.Services.Dialog;

namespace Easymakemoney.ViewModels.Lists
{
    /// <summary>
    /// Frais fixes (lot backend FF1) : loyer, salaires, assurances, abonnements, énergie… avec leur équivalent mensuel,
    /// le total par mois et par catégorie. Ce sont des mouvements prévus de la trésorerie : la saisie réutilise leur formulaire.
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

        public ObservableCollection<FixedCostItem> Items { get; } = new();

        public ObservableCollection<FixedCostCategory> Categories { get; } = new();

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
                var list = await _treasuryService.GetFixedCostsAsync(all: true);
                Items.Clear();
                foreach (var item in list.SortedItems(DateTime.Today))
                    Items.Add(item);
                Categories.Clear();
                foreach (var category in list.Categories())
                    Categories.Add(category);
                IsEmpty = Items.Count == 0;
                MonthlyTotalText = list.MonthlyTotalText;
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
        private async Task Delete(FixedCostItem? item)
        {
            if (item == null || !item.CanDelete)
                return;

            var confirmed = await _dialogService.ConfirmAsync("Supprimer ce frais fixe", $"Supprimer « {item.label} » et ses prochaines échéances ?", "Supprimer", "Annuler");
            if (!confirmed)
                return;

            try
            {
                await _treasuryService.DeleteMovementAsync(item.id);
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
