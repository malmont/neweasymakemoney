using System.Globalization;
using Easymakemoney.Services.Dialog;
using RepriseUseCaseService = Easymakemoney.UseCase.RepriseUseCase.RepriseUseCase;

namespace Easymakemoney.ViewModels.Lists
{
    public partial class RepriseViewModel : BaseViewModel
    {
        private const int PageSize = 100;

        private readonly RepriseUseCaseService _repriseUseCase;
        private readonly IDialogService _dialogService;
        private readonly Dictionary<int, RepriseProduct> _edited = new();
        private int _page;

        public RepriseViewModel(RepriseUseCaseService repriseUseCase, IDialogService dialogService)
        {
            _repriseUseCase = repriseUseCase;
            _dialogService = dialogService;
        }

        public ObservableCollection<RepriseProduct> Products { get; } = new ObservableCollection<RepriseProduct>();

        public ObservableCollection<RepriseCommande> Commandes { get; } = new ObservableCollection<RepriseCommande>();

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ShowSimulation))]
        [NotifyPropertyChangedFor(nameof(HasSalesToResume))]
        [NotifyPropertyChangedFor(nameof(HasNothingToResume))]
        private RepriseState? state;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasSimulation))]
        [NotifyPropertyChangedFor(nameof(ShowSimulation))]
        [NotifyPropertyChangedFor(nameof(HasSalesToResume))]
        [NotifyPropertyChangedFor(nameof(HasNothingToResume))]
        private RepriseSimulation? simulation;

        [ObservableProperty]
        private int months = 12;

        [ObservableProperty]
        private string? coefficientText;

        [ObservableProperty]
        private bool hasMoreProducts;

        public bool IsActive => State?.active == true;
        public bool HasSimulation => Simulation != null;
        public bool ShowSimulation => State != null && !IsActive && HasSimulation;
        public bool HasSalesToResume => ShowSimulation && Simulation!.productsTotal > 0;
        public bool HasNothingToResume => ShowSimulation && Simulation!.productsTotal == 0;

        public async Task LoadAsync()
        {
            try
            {
                State = await _repriseUseCase.GetStateAsync();
                OnPropertyChanged(nameof(IsActive));
                Commandes.Clear();
                foreach (var commande in State.commandes ?? new List<RepriseCommande>())
                {
                    Commandes.Add(commande);
                }

                if (!IsActive)
                {
                    await SimulateAsync();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                await _dialogService.ShowErrorAsync(e);
            }
        }

        [RelayCommand]
        private async Task SetMonths(string? value)
        {
            if (int.TryParse(value, out var parsed))
            {
                Months = parsed;
                await SimulateAsync();
            }
        }

        [RelayCommand]
        private async Task Refresh()
        {
            if (TryReadCoefficient(out _))
            {
                await SimulateAsync();
            }
            else
            {
                await _dialogService.ShowInfoAsync("Coefficient invalide.");
            }
        }

        private async Task SimulateAsync()
        {
            if (IsBusy || !TryReadCoefficient(out var coefficient))
                return;

            IsBusy = true;
            try
            {
                var result = await _repriseUseCase.SimulateAsync(Months, coefficient, 1, PageSize);
                Products.Clear();
                AddProducts(result);
                _page = 1;
                Simulation = result;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                await _dialogService.ShowErrorAsync(e);
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task LoadMore()
        {
            if (IsBusy || !TryReadCoefficient(out var coefficient))
                return;

            IsBusy = true;
            try
            {
                var result = await _repriseUseCase.SimulateAsync(Months, coefficient, _page + 1, PageSize);
                AddProducts(result);
                _page++;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                await _dialogService.ShowErrorAsync(e);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void AddProducts(RepriseSimulation result)
        {
            foreach (var product in result.products)
            {
                if (_edited.TryGetValue(product.productId, out var edited))
                {
                    product.EditedCostCents = edited.EditedCostCents;
                }
                Products.Add(product);
            }

            HasMoreProducts = Products.Count < result.productsTotal;
        }

        [RelayCommand]
        private async Task EditCost(RepriseProduct? product)
        {
            var page = Application.Current?.MainPage;
            if (product == null || page == null || !product.CanEditCost)
                return;

            var text = await page.DisplayPromptAsync("Coût d'achat", product.name, "Valider", "Annuler", keyboard: Keyboard.Numeric);
            if (string.IsNullOrWhiteSpace(text))
                return;

            if (!TryParsePositive(text, out var amount))
            {
                await _dialogService.ShowInfoAsync("Montant invalide.");
                return;
            }

            product.EditedCostCents = (long)Math.Round((decimal)amount * 100m, MidpointRounding.AwayFromZero);
            _edited[product.productId] = product;
        }

        [RelayCommand]
        private async Task Apply()
        {
            if (IsBusy || !TryReadCoefficient(out var coefficient))
                return;

            bool confirmed = await _dialogService.ConfirmAsync(
                "Reprendre l'historique",
                "Les périodes passées seront recalculées. Vos prix, votre stock et vos ventes ne changent pas. Vous pourrez annuler.",
                "Reprendre",
                "Annuler");
            if (!confirmed)
                return;

            IsBusy = true;
            try
            {
                await _repriseUseCase.ApplyAsync(Months, coefficient, _edited.Values);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                await _dialogService.ShowErrorAsync(e);
                return;
            }
            finally
            {
                IsBusy = false;
            }

            _edited.Clear();
            await LoadAsync();
            await _dialogService.ShowInfoAsync("Historique repris.");
        }

        [RelayCommand]
        private async Task Cancel()
        {
            if (IsBusy)
                return;

            bool confirmed = await _dialogService.ConfirmAsync(
                "Annuler la reprise",
                "Les commandes de reprise seront supprimées et les périodes passées recalculées.",
                "Annuler la reprise",
                "Conserver");
            if (!confirmed)
                return;

            IsBusy = true;
            try
            {
                await _repriseUseCase.CancelAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                await _dialogService.ShowErrorAsync(e);
                return;
            }
            finally
            {
                IsBusy = false;
            }

            await LoadAsync();
            await _dialogService.ShowInfoAsync("Reprise annulée.");
        }

        private bool TryReadCoefficient(out double? coefficient)
        {
            coefficient = null;
            if (string.IsNullOrWhiteSpace(CoefficientText))
                return true;

            if (!TryParsePositive(CoefficientText, out var value) || value <= 0)
                return false;

            coefficient = value;
            return true;
        }

        private static bool TryParsePositive(string text, out double value) =>
            double.TryParse(text.Trim().Replace(',', '.'), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out value);
    }
}
