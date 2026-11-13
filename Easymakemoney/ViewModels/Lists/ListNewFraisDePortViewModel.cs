
using Easymakemoney.Services.Dialog;
using Microsoft.Extensions.DependencyInjection;

namespace Easymakemoney.ViewModels.Lists
{
    public partial class ListNewFraisDePortViewModel : BaseViewModel
    {
        private const int MaxConcurrentRequests = 4;

        private readonly GetListCollectionsUseCase _getListCollectionsUseCase;
        private readonly GetListCommandUseCase _getListCommandUseCase;
        private readonly GetListFraisDePortUseCase _getListFraisDePortUseCase;
        private readonly DeleteFraisDePortUseCase _deleteFraisDePortUseCase;
        private readonly SaveFraisDePortUseCase _saveFraisDePortUseCase;
        private readonly Easymakemoney.UseCase.PeriodResultUseCase.PeriodUseCase _periodUseCase;
        private readonly IServiceProvider _services;
        private readonly IDialogService _dialogService;
        private readonly IManagementEditService? _editService;

        // Une seule réponse doit remplir l'écran : celle de la dernière collection choisie.
        private int _loadVersion;

        public ListNewFraisDePortViewModel(GetListCollectionsUseCase getListCollectionsUseCase,
            GetListCommandUseCase getListCommandUseCase, GetListFraisDePortUseCase getListFraisDePortUseCase,
            DeleteFraisDePortUseCase deleteFraisDePortUseCase, SaveFraisDePortUseCase saveFraisDePortUseCase,
            Easymakemoney.UseCase.PeriodResultUseCase.PeriodUseCase periodUseCase,
            IServiceProvider services, IDialogService dialogService, IManagementEditService? editService = null)
        {
            _editService = editService;
            _periodUseCase = periodUseCase;
            _getListCollectionsUseCase = getListCollectionsUseCase;
            _getListCommandUseCase = getListCommandUseCase;
            _getListFraisDePortUseCase = getListFraisDePortUseCase;
            _deleteFraisDePortUseCase = deleteFraisDePortUseCase;
            _saveFraisDePortUseCase = saveFraisDePortUseCase;
            _services = services;
            _dialogService = dialogService;
        }

        public ObservableCollection<FraisDePortCommandeRow> Rows { get; } = new ObservableCollection<FraisDePortCommandeRow>();

        public int CollectionId { get; set; }

        /// <summary>Commande visée par le formulaire d'ajout en cours.</summary>
        public int CommandId { get; set; }

        [ObservableProperty]
        private string? collectionImage;

        [ObservableProperty]
        private string? collectionName;

        [ObservableProperty]
        private bool isClosed;

        [ObservableProperty]
        private bool showEmptyState = true;

        [ObservableProperty]
        private string emptyTitle = "Choisissez une période";

        [ObservableProperty]
        private string emptyMessage = "Ses commandes apparaîtront ici, avec leurs frais de port.";

        /// <summary>À l'ouverture de l'écran, sans choix préalable : la période en cours.</summary>
        public async Task SelectCurrentPeriodIfNoneAsync()
        {
            if (CollectionId != 0)
                return;

            try
            {
                var current = await _periodUseCase.GetCurrentPeriodAsync();
                if (current != null && CollectionId == 0)
                {
                    await SelectCollectionAsync(current);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                await _dialogService.ShowErrorAsync(e);
            }
        }

        private async Task SelectCollectionAsync(ListCollection collection)
        {
            CollectionId = collection.id;
            CollectionImage = collection.Photo;
            CollectionName = collection.Name;
            IsClosed = collection.IsClosed;
            await GetListFraisDePort();
        }

        public async Task GetListFraisDePort()
        {
            var version = ++_loadVersion;
            Rows.Clear();

            if (CollectionId == 0)
            {
                SetEmptyState("Choisissez une période", "Ses commandes apparaîtront ici, avec leurs frais de port.");
                return;
            }

            SetEmptyState("Chargement…", string.Empty);
            IsBusy = true;

            try
            {
                var commandes = await _getListCommandUseCase.ExecuteAsync(CollectionId);
                var rows = await LoadPeriodRowsAsync(commandes ?? new ObservableCollection<ListCommand>())
                           ?? await LoadRowsOneByOneAsync(commandes ?? new ObservableCollection<ListCommand>());

                if (version != _loadVersion)
                    return;

                foreach (var row in rows)
                {
                    Rows.Add(row);
                }

                SetEmptyState("Aucune commande", "Cette période n'a pas encore de commande.");
                ShowEmptyState = Rows.Count == 0;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                if (version != _loadVersion)
                    return;

                SetEmptyState("Chargement impossible", "Choisissez de nouveau la période pour réessayer.");
                await _dialogService.ShowErrorAsync(e);
            }
            finally
            {
                if (version == _loadVersion)
                    IsBusy = false;
            }
        }

        /// <summary>
        /// Lot B3 bis : les frais de port de toute la période en une lecture (montants convertis par le serveur). Null si la lecture
        /// n'est pas disponible : l'écran revient alors à une lecture par commande.
        /// </summary>
        private async Task<FraisDePortCommandeRow[]?> LoadPeriodRowsAsync(IEnumerable<ListCommand> commandes)
        {
            if (_editService == null)
                return null;
            try
            {
                var period = await _editService.GetPeriodShippingAsync(CollectionId);
                if (period == null)
                    return null;
                var byCommande = period.commandes.ToDictionary(c => c.commandeId, c => c.fraisDePort);
                return commandes
                    .Select(commande => new FraisDePortCommandeRow
                    {
                        Commande = commande,
                        Frais = byCommande.TryGetValue(commande.id, out var frais) ? frais : null,
                        PeriodClosed = IsClosed,
                    })
                    .ToArray();
            }
            catch (Exception e)
            {
                Debug.WriteLine($"[FraisDePort] Lecture de la période indisponible, une lecture par commande : {e.Message}");
                return null;
            }
        }

        private async Task<FraisDePortCommandeRow[]> LoadRowsOneByOneAsync(IEnumerable<ListCommand> commandes)
        {
            using var gate = new SemaphoreSlim(MaxConcurrentRequests);
            return await Task.WhenAll(commandes.Select(commande => LoadRowAsync(commande, gate)));
        }

        private async Task<FraisDePortCommandeRow> LoadRowAsync(ListCommand commande, SemaphoreSlim gate)
        {
            await gate.WaitAsync();
            try
            {
                var frais = await _getListFraisDePortUseCase.ExecuteAsync(commande.id);
                return new FraisDePortCommandeRow { Commande = commande, Frais = frais.FirstOrDefault(), PeriodClosed = IsClosed };
            }
            finally
            {
                gate.Release();
            }
        }

        private void SetEmptyState(string title, string message)
        {
            EmptyTitle = title;
            EmptyMessage = message;
            ShowEmptyState = true;
        }

        [RelayCommand]
        private async Task DeleteFraisDePort(FraisDePortCommandeRow? row)
        {
            if (row?.Frais == null || IsBusy)
                return;

            bool confirmed = await _dialogService.ConfirmAsync(
                "Confirmation",
                "Êtes-vous sûr de vouloir supprimer ce frais de port ?",
                "Supprimer",
                "Annuler");
            if (!confirmed)
                return;

            IsBusy = true;
            try
            {
                var result = await _deleteFraisDePortUseCase.ExecuteAsync(row.Commande.id);
                if (!result)
                {
                    await _dialogService.ShowInfoAsync("Échec de la suppression du frais de port.");
                    return;
                }
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

            await GetListFraisDePort();
            await _dialogService.ShowInfoAsync("Frais de port supprimé avec succès.");
        }

        /// <summary>Lot EDIT : ouvre la modification du frais de port d'une commande (période ouverte).</summary>
        [RelayCommand]
        private async Task EditFrais(FraisDePortCommandeRow? row)
        {
            if (row == null || !row.CanEditFrais || IsBusy)
                return;
            await NavigateAsync(nameof(ShippingEditPage), new Dictionary<string, object> { ["Row"] = row });
        }

        /// <summary>Navigation (point d'entrée des tests).</summary>
        public Func<string, IDictionary<string, object>, Task> NavigateAsync { get; set; } =
            (route, parameters) => Shell.Current?.GoToAsync(route, new ShellNavigationQueryParameters(parameters)) ?? Task.CompletedTask;

        [RelayCommand]
        private async Task ReadTransportInvoice(FraisDePortCommandeRow? row)
        {
            if (row == null || !row.CanAddFrais)
                return;

            if (Shell.Current is { } shell)
                await shell.GoToAsync($"{nameof(DocumentReadPage)}?Type=transport&CommandeId={row.Commande.id}", true);
        }

        /// <summary>Au retour sur l'écran : un frais a pu être créé par la lecture d'une facture.</summary>
        public async Task RefreshAsync()
        {
            if (CollectionId == 0)
                await SelectCurrentPeriodIfNoneAsync();
            else
                await GetListFraisDePort();
        }

        [RelayCommand]
        private async Task AddFrais(FraisDePortCommandeRow? row)
        {
            if (row == null || row.HasFrais || IsBusy)
                return;

            CommandId = row.Commande.id;

            // Un formulaire neuf à chaque ajout : rien ne reste de la commande précédente.
            var viewModel = new BottomSheetPopupViewModel(
                popup: new Popup(),
                fraisDePortViewModel: this,
                isFraisDePortForm: true,
                saveFraisDePortUseCase: _saveFraisDePortUseCase,
                fraisDePortForm: _services.GetRequiredService<FraisDePortFormModel>(),
                dialogService: _dialogService);

            var popup = new BottomSheetPopup(viewModel);
            var mainPage = Easymakemoney.Utilities.AppPage.Current;

            if (mainPage != null)
            {
                await mainPage.ShowPopupAsync(popup);
            }
        }

        [RelayCommand]
        public async Task ShowBottomSheetPopupListView()
        {
            var viewModel = new BottomSheetPopupListViewViewModel(
                getItemsFunc: async () => await _getListCollectionsUseCase.ExecuteAsync(),
                onItemTappedAction: async (item) =>
                {
                    if (item is ListCollection collection)
                    {
                        await SelectCollectionAsync(collection);
                    }
                });

            var popup = new BottomSheetPopupListView(viewModel);
            if (Easymakemoney.Utilities.AppPage.Current != null)
                await Easymakemoney.Utilities.AppPage.Current!.ShowPopupAsync(popup);
        }
    }
}
