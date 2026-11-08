namespace Easymakemoney.ViewModels.Statistiques
{
    public partial class StatistiquesViewModel
    {
        public StatistiquesViewModel()
        {
            
        }

         [RelayCommand]
        async void NavigateToChiffreAffaire()
        {
            await Shell.Current.GoToAsync(nameof(ChiffresAffairePage));
        }
         [RelayCommand]
        async void NavigateToPurchasing()
        {
            await Shell.Current.GoToAsync(nameof(OrderNumberPage));
        }
         [RelayCommand]
        async void NavigateToPanier()
        {
            await Shell.Current.GoToAsync(nameof(PanierMoyenPage));
        }
         [RelayCommand]
        async void NavigateToStock()
        {
            await Shell.Current.GoToAsync(nameof(StockPage));
        }
         [RelayCommand]
        async void NavigateToFrais()
        {
            await Shell.Current.GoToAsync(nameof(FraisEvolutionEntreprisePage));
        }
         [RelayCommand]
        async void NavigateToCaisse()
        {
            await Shell.Current.GoToAsync(nameof(TransactionCaissePage));
        }
         [RelayCommand]
        async void NavigateToPayment()
        {
            await Shell.Current.GoToAsync(nameof(PaiementRemboursementPage));
        }

          [RelayCommand]
        async void NavigateToFraisTotal()
        {
            await Shell.Current.GoToAsync(nameof(FraisClientTotalViewPage));
        }
    }
}