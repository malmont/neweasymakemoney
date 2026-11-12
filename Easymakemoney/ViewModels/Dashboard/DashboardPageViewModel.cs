

using System.Globalization;
using Easymakemoney.Views.Statistiques;

namespace Easymakemoney.ViewModels.Dashboard
{
    public partial class DashboardPageViewModel : BaseViewModel
    {
        private readonly IHomeTilesService _tilesService;
        private readonly IManagementModuleService? _moduleService;
        private readonly Services.Offline.IHomeSnapshotStore? _snapshots;
        private readonly Func<string?>? _scope;
        private bool _loadingTiles;

        public DashboardPageViewModel(IHomeTilesService tilesService, IManagementModuleService? moduleService = null,
            Services.Offline.IHomeSnapshotStore? snapshots = null, Services.Tenant.ITenantStore? tenants = null,
            Services.Session.ISessionService? session = null)
            : this(tilesService, moduleService, snapshots, () => SnapshotScope(tenants?.CurrentTenant?.Code, session?.CurrentUser?.Id))
        {
        }

        public DashboardPageViewModel(IHomeTilesService tilesService, IManagementModuleService? moduleService,
            Services.Offline.IHomeSnapshotStore? snapshots, Func<string?>? scope)
        {
            _tilesService = tilesService;
            _moduleService = moduleService;
            _snapshots = snapshots;
            _scope = scope;
            Title = "welcome";
        }

        /// <summary>Copie locale par site et par compte ; sans site ni compte connus, pas de copie.</summary>
        public static string? SnapshotScope(string? tenantCode, int? userId) =>
            string.IsNullOrWhiteSpace(tenantCode) || userId is not > 0 ? null : $"{tenantCode.Trim().ToLowerInvariant()}:{userId}";

        /// <summary>« Hors connexion : données de 14 h 05 », quand l'Accueil vient de la copie locale.</summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsOffline))]
        private string _offlineText = string.Empty;

        public bool IsOffline => !string.IsNullOrEmpty(OfflineText);

        /// <summary>Lot CHE : carte « Préparer ma gestion » en tête de l'accueil, tant que la configuration n'est pas complète.</summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasReadinessCard), nameof(ReadinessCardText), nameof(IsReadinessAlert))]
        private ManagementReadinessSummary? _readiness;

        /// <summary>Lot JOUR : « Ma journée », cachée tant que la route n'existe pas ou en cas d'erreur.</summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasDay))]
        private DaySummary? _day;

        public bool HasDay => Day?.sales != null;

        public Func<string, Task> NavigateAsync { get; set; } = route => Shell.Current?.GoToAsync(route) ?? Task.CompletedTask;

        private async Task<DaySummary?> LoadDayAsync()
        {
            if (_moduleService == null)
                return null;
            try
            {
                return await _moduleService.GetDayAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ma journée indisponible : {ex.Message}");
                return null;
            }
        }

        /// <summary>Une rubrique de « Ma journée » ouvre son écran ; sans écran dans l'appli, rien ne se passe.</summary>
        [RelayCommand]
        private async Task OpenDay(string part)
        {
            var route = DayRoute(Day?.Screen(part) ?? part);
            if (route != null)
                await NavigateAsync(route);
        }

        public static string? DayRoute(string? screen) => screen switch
        {
            "statistiques" => "StatistiquesPage?Range=today",
            "tresorerie" => "TreasuryPage",
            "assistance" => "SupportTicketListPage",
            _ => null,
        };

        public bool HasReadinessCard => Readiness?.HasHomeCard == true;
        public bool IsReadinessAlert => Readiness?.IsHomeCardAlert == true;
        public string ReadinessCardText => Readiness?.HomeCardText ?? string.Empty;

        /// <summary>Lecture silencieuse : sans cheminement (erreur, module absent), la carte reste cachée.</summary>
        private async Task<ManagementReadinessSummary?> LoadReadinessAsync()
        {
            if (_moduleService == null)
                return null;
            try
            {
                var steps = (await _moduleService.GetReadinessAsync()).ManagementSteps;
                return steps.Count > 0 ? new ManagementReadinessSummary(steps) : null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Cheminement indisponible : {ex.Message}");
                return null;
            }
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
                var readinessTask = LoadReadinessAsync();
                var dayTask = LoadDayAsync();
                // L'application est en français : les textes du backend doivent l'être aussi, quelle que soit la langue du téléphone.
                var tiles = await _tilesService.GetTilesAsync("fr");
                Readiness = await readinessTask;
                Day = await dayTask;
                // La carte « Préparer ma gestion » dit déjà de saisir le premier solde : pas de redite.
                if (HasReadinessCard)
                    tiles = tiles.Where(tile => tile.id != ManagementReadinessSummary.CashFirstBalanceTile).ToList();
                Tiles.Clear();
                foreach (var tile in tiles.OrderByDescending(tile => tile.priority).Take(3))
                    Tiles.Add(tile);
                OfflineText = string.Empty;
                SaveSnapshot();
            }
            catch (Services.Api.ApiException ex) when (ex.ApiError.Code == "NO_CONNECTION" && LoadSnapshot())
            {
                Debug.WriteLine($"Accueil hors connexion, copie locale affichée : {ex.Message}");
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

        private void SaveSnapshot()
        {
            if (_snapshots == null || _scope?.Invoke() is not { } scope)
                return;
            _snapshots.Save(scope, new Services.Offline.HomeSnapshot
            {
                SavedAt = DateTime.Now,
                Tiles = Tiles.ToList(),
                Day = Day,
                ReadinessSteps = Readiness == null ? new() : Readiness.Blocking.Concat(Readiness.Improvements).Concat(Readiness.Done).ToList(),
            });
        }

        /// <summary>Sans réseau : la dernière copie de l'Accueil, datée. Faux s'il n'y en a pas.</summary>
        private bool LoadSnapshot()
        {
            if (_snapshots == null || _scope?.Invoke() is not { } scope || _snapshots.Load(scope) is not { } snapshot)
                return false;
            Tiles.Clear();
            foreach (var tile in snapshot.Tiles)
                Tiles.Add(tile);
            Day = snapshot.Day;
            Readiness = snapshot.ReadinessSteps.Count > 0 ? new ManagementReadinessSummary(snapshot.ReadinessSteps) : null;
            var culture = CultureInfo.GetCultureInfo("fr-CA");
            OfflineText = snapshot.SavedAt.Date == DateTime.Today
                ? $"Hors connexion : données de {snapshot.SavedAt.ToString("H' h 'mm", culture)}"
                : $"Hors connexion : données du {Utilities.FrenchDate.Format(snapshot.SavedAt, "d MMMM 'à' H' h 'mm")}";
            return true;
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
        private async Task OpenReadiness()
        {
            if (Shell.Current is { } shell)
                await shell.GoToAsync(nameof(ManagementReadinessPage));
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

