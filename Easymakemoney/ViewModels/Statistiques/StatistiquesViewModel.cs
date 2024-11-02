namespace Easymakemoney.ViewModels.Statistiques
{
    public partial class StatistiquesViewModel
    {
        public StatistiquesViewModel()
        {
            
        }

         [ICommand]
        async void NavigateToChiffreAffaire()
        {
            await Shell.Current.GoToAsync(nameof(ChiffresAffairePage));
        }
         [ICommand]
        async void NavigateToPurchasing()
        {
            await Shell.Current.GoToAsync(nameof(OrderNumberPage));
        }
         [ICommand]
        async void NavigateToPanier()
        {
            await Shell.Current.GoToAsync(nameof(PanierMoyenPage));
        }
         [ICommand]
        async void NavigateToStock()
        {
            await Shell.Current.GoToAsync(nameof(StockPage));
        }
         [ICommand]
        async void NavigateToFrais()
        {
            await Shell.Current.GoToAsync(nameof(FraisEvolutionEntreprisePage));
        }
         [ICommand]
        async void NavigateToCaisse()
        {
            await Shell.Current.GoToAsync(nameof(TransactionCaissePage));
        }
         [ICommand]
        async void NavigateToPayment()
        {
            await Shell.Current.GoToAsync(nameof(PaiementRemboursementPage));
        }

          [ICommand]
        async void NavigateToFraisTotal()
        {
            await Shell.Current.GoToAsync(nameof(FraisClientTotalViewPage));
        }
    }
}