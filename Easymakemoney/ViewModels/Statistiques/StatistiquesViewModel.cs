namespace Easymakemoney.ViewModels.Statistiques
{
    public partial class StatistiquesViewModel
    {
        public StatistiquesViewModel()
        {
            
        }

        [RelayCommand]
        private async Task NavigateToChiffreAffaire()
        {
            if (Shell.Current is { } shell)
                await shell.GoToAsync(nameof(ChiffresAffairePage));
        }

        [RelayCommand]
        private async Task NavigateToPurchasing()
        {
            if (Shell.Current is { } shell)
                await shell.GoToAsync(nameof(OrderNumberPage));
        }

        [RelayCommand]
        private async Task NavigateToPanier()
        {
            if (Shell.Current is { } shell)
                await shell.GoToAsync(nameof(PanierMoyenPage));
        }

        [RelayCommand]
        private async Task NavigateToStock()
        {
            if (Shell.Current is { } shell)
                await shell.GoToAsync(nameof(StockPage));
        }

        [RelayCommand]
        private async Task NavigateToFrais()
        {
            if (Shell.Current is { } shell)
                await shell.GoToAsync(nameof(FraisEvolutionEntreprisePage));
        }

        [RelayCommand]
        private async Task NavigateToCaisse()
        {
            if (Shell.Current is { } shell)
                await shell.GoToAsync(nameof(TransactionCaissePage));
        }

        [RelayCommand]
        private async Task NavigateToPayment()
        {
            if (Shell.Current is { } shell)
                await shell.GoToAsync(nameof(PaiementRemboursementPage));
        }

        [RelayCommand]
        private async Task NavigateToFraisTotal()
        {
            if (Shell.Current is { } shell)
                await shell.GoToAsync(nameof(FraisClientTotalViewPage));
        }
    }
}