

using System.Globalization;
using Easymakemoney.Views.Statistiques;

namespace Easymakemoney.ViewModels.Dashboard
{
    public partial class DashboardPageViewModel : BaseViewModel
    {
        private readonly IHomeTilesService _tilesService;
        private bool _loadingTiles;

        public DashboardPageViewModel(IHomeTilesService tilesService)
        {
            _tilesService = tilesService;
            Title = "welcome";
        }

        /// <summary>Vignettes d'accueil, les plus urgentes d'abord (3 au plus).</summary>
        public ObservableCollection<HomeTile> Tiles { get; } = new();

        [ObservableProperty]
        private bool _hasTiles;

        /// <summary>Recharge les vignettes à chaque retour sur l'accueil ; une erreur les masque sans gêner le reste de l'écran.</summary>
        public async Task LoadTilesAsync()
        {
            if (_loadingTiles)
                return;

            _loadingTiles = true;
            try
            {
                // L'application est en français : les textes du backend doivent l'être aussi, quelle que soit la langue du téléphone.
                var tiles = await _tilesService.GetTilesAsync("fr");
                Tiles.Clear();
                foreach (var tile in tiles.OrderByDescending(tile => tile.priority).Take(3))
                    Tiles.Add(tile);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Vignettes d'accueil indisponibles : {ex.Message}");
                Tiles.Clear();
            }
            finally
            {
                HasTiles = Tiles.Count > 0;
                _loadingTiles = false;
            }
        }

        [RelayCommand]
        private async Task OpenTile(HomeTile? tile)
        {
            if (tile == null)
                return;

            // Un écran pas encore présent dans cette version ne fait rien : la vignette reste informative.
            await Utilities.ScreenNavigator.OpenAsync(tile.Screen, tile.action?.@params, tile.figures, tile.title, tile.text, tile: tile);
        }

        [RelayCommand]
        private async Task OpenGuide()
        {
            if (Shell.Current is { } shell)
                await shell.GoToAsync(nameof(AdvicePage));
        }

        [RelayCommand]
        private async Task DismissTile(HomeTile? tile)
        {
            if (tile == null || !tile.dismissible)
                return;

            try
            {
                await _tilesService.DismissAsync(tile.id);
                Tiles.Remove(tile);
                HasTiles = Tiles.Count > 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Vignette non écartée : {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task NavigateToAllListsPage()
        {
            if (Shell.Current is { } shell)
                await shell.GoToAsync(nameof(AllListsPage));
        }

        [RelayCommand]
        private async Task NavigateTDashboardPage()
        {
            if (Shell.Current is { } shell)
                await shell.GoToAsync(nameof(DashBoardCollectionPage)); 
        }

        [RelayCommand]
        private async Task NavigateToStatistiquesPage()
        {
            if (Shell.Current is { } shell)
                await shell.GoToAsync(nameof(StatistiquesPage));
        }
    }
}

