
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
        private readonly IServiceProvider _services;
        private readonly IDialogService _dialogService;

        // Une seule réponse doit remplir l'écran : celle de la dernière collection choisie.
        private int _loadVersion;

        public ListNewFraisDePortViewModel(GetListCollectionsUseCase getListCollectionsUseCase,
            GetListCommandUseCase getListCommandUseCase, GetListFraisDePortUseCase getListFraisDePortUseCase,
            DeleteFraisDePortUseCase deleteFraisDePortUseCase, SaveFraisDePortUseCase saveFraisDePortUseCase,
            IServiceProvider services, IDialogService dialogService)
        {
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
        public int CommandId { get; private set; }

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
                using var gate = new SemaphoreSlim(MaxConcurrentRequests);
                var rows = await Task.WhenAll((commandes ?? new ObservableCollection<ListCommand>())
                    .Select(commande => LoadRowAsync(commande, gate)));

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
            var mainPage = Application.Current?.MainPage;

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
                        CollectionId = collection.id;
                        CollectionImage = collection.Photo;
                        CollectionName = collection.Name;
                        IsClosed = collection.IsClosed;
                        await GetListFraisDePort();
                    }
                });

            var popup = new BottomSheetPopupListView(viewModel);
            if (Application.Current?.MainPage != null)
                await Application.Current.MainPage.ShowPopupAsync(popup);
        }
    }
}
