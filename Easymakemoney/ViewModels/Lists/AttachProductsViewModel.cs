using System.Globalization;
using Easymakemoney.Services.Dialog;
using Easymakemoney.UseCase.ProductAttachmentUseCase;

namespace Easymakemoney.ViewModels.Lists
{
    public partial class AttachProductsViewModel : BaseViewModel
    {
        private const int PageSize = 30;
        private const int SearchDelayMs = 300;

        private readonly GetAvailableProductsUseCase _getAvailableProductsUseCase;
        private readonly AttachProductsUseCase _attachProductsUseCase;
        private readonly GetListCollectionsUseCase _getListCollectionsUseCase;
        private readonly GetListCommandUseCase _getListCommandUseCase;
        private readonly IDialogService _dialogService;

        // La sélection survit à un changement de recherche.
        private readonly Dictionary<int, AvailableProduct> _selected = new();
        private CancellationTokenSource? _searchDelay;
        private int _loadVersion;
        private int _page;
        private int _total;
        private bool _isLoadingMore;

        public AttachProductsViewModel(GetAvailableProductsUseCase getAvailableProductsUseCase,
            AttachProductsUseCase attachProductsUseCase, GetListCollectionsUseCase getListCollectionsUseCase,
            GetListCommandUseCase getListCommandUseCase, IDialogService dialogService)
        {
            _getListCollectionsUseCase = getListCollectionsUseCase;
            _getListCommandUseCase = getListCommandUseCase;
            _getAvailableProductsUseCase = getAvailableProductsUseCase;
            _attachProductsUseCase = attachProductsUseCase;
            _dialogService = dialogService;
        }

        public ObservableCollection<AvailableProduct> Products { get; } = new ObservableCollection<AvailableProduct>();

        /// <summary>0 : mode « À classer », la commande est choisie à la validation.</summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(PageTitle))]
        [NotifyPropertyChangedFor(nameof(ConfirmText))]
        [NotifyPropertyChangedFor(nameof(IsClassifying))]
        private int commandId;

        public bool IsClassifying => CommandId == 0;

        /// <summary>Réassort : tout le catalogue, y compris les articles déjà achetés dans une autre commande.</summary>
        [ObservableProperty]
        private bool showAllProducts;

        partial void OnShowAllProductsChanged(bool value) => _ = LoadFirstPageAsync();

        public string PageTitle => IsClassifying ? "À classer" : "Articles existants";

        [ObservableProperty]
        private string? searchText;

        [ObservableProperty]
        private bool showEmptyState;

        [ObservableProperty]
        private string emptyTitle = "Chargement…";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasSelection))]
        [NotifyPropertyChangedFor(nameof(ConfirmText))]
        private int selectedCount;

        public bool HasSelection => SelectedCount > 0;

        public string ConfirmText => SelectedCount > 1
            ? $"{(IsClassifying ? "Associer" : "Ajouter")} {SelectedCount} articles"
            : $"{(IsClassifying ? "Associer" : "Ajouter")} 1 article";

        partial void OnSearchTextChanged(string? value) => _ = SearchAfterDelayAsync();

        private async Task SearchAfterDelayAsync()
        {
            _searchDelay?.Cancel();
            var delay = _searchDelay = new CancellationTokenSource();
            try
            {
                await Task.Delay(SearchDelayMs, delay.Token);
            }
            catch (TaskCanceledException)
            {
                return;
            }

            await LoadFirstPageAsync();
        }

        public async Task LoadFirstPageAsync()
        {
            var version = ++_loadVersion;
            EmptyTitle = "Chargement…";
            ShowEmptyState = Products.Count == 0;

            try
            {
                var result = await _getAvailableProductsUseCase.ExecuteAsync(SearchText, 1, PageSize, ShowAllProducts && !IsClassifying);
                if (version != _loadVersion)
                    return;

                Products.Clear();
                AddResults(result);
                _page = 1;
                EmptyTitle = IsClassifying ? "Aucun article à classer" : "Aucun article disponible";
                ShowEmptyState = Products.Count == 0;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                if (version != _loadVersion)
                    return;

                EmptyTitle = "Chargement impossible";
                ShowEmptyState = Products.Count == 0;
                await _dialogService.ShowErrorAsync(e);
            }
        }

        [RelayCommand]
        private async Task LoadMore()
        {
            if (_isLoadingMore || _page == 0 || Products.Count >= _total)
                return;

            _isLoadingMore = true;
            var version = _loadVersion;
            try
            {
                var result = await _getAvailableProductsUseCase.ExecuteAsync(SearchText, _page + 1, PageSize, ShowAllProducts && !IsClassifying);
                if (version != _loadVersion)
                    return;

                AddResults(result);
                _page++;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                await _dialogService.ShowErrorAsync(e);
            }
            finally
            {
                _isLoadingMore = false;
            }
        }

        private void AddResults(AvailableProductsPage result)
        {
            _total = result.total;
            foreach (var product in result.items)
            {
                Products.Add(_selected.TryGetValue(product.id, out var selected) ? selected : product);
            }
        }

        [RelayCommand]
        private void ToggleProduct(AvailableProduct? product)
        {
            if (product != null)
                SetSelected(product, !product.IsSelected);
        }

        private void SetSelected(AvailableProduct product, bool selected)
        {
            product.IsSelected = selected;
            if (selected)
                _selected[product.id] = product;
            else
                _selected.Remove(product.id);

            SelectedCount = _selected.Count;
        }

        [RelayCommand]
        private async Task EditCost(AvailableProduct? product)
        {
            if (product == null)
                return;

            var choice = await _dialogService.ChooseAsync(product.name ?? "Article", new[] { "Coût d'achat", "Quantité achetée" });
            if (choice == "Quantité achetée")
            {
                var quantityText = await _dialogService.PromptAsync("Quantité achetée", product.name ?? string.Empty, keyboard: Keyboard.Numeric);
                if (string.IsNullOrWhiteSpace(quantityText))
                    return;
                if (!int.TryParse(quantityText.Trim(), out var quantity) || quantity <= 0)
                {
                    await _dialogService.ShowInfoAsync("Quantité invalide.");
                    return;
                }
                product.EditedQuantity = quantity;
                SetSelected(product, true);
                return;
            }
            if (choice != "Coût d'achat")
                return;

            var text = await _dialogService.PromptAsync("Coût d'achat", product.name ?? string.Empty, keyboard: Keyboard.Numeric);

            if (string.IsNullOrWhiteSpace(text))
                return;

            if (!double.TryParse(text.Trim().Replace(',', '.'), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var amount))
            {
                await _dialogService.ShowInfoAsync("Montant invalide.");
                return;
            }

            product.EditedCostCents = (long)Math.Round((decimal)amount * 100m, MidpointRounding.AwayFromZero);
            SetSelected(product, true);
        }

        private async Task<int?> PickCommandeAsync()
        {
            try
            {
                ListCollection? period = null;
                await ShowPickerAsync(
                    async () => (await _getListCollectionsUseCase.ExecuteAsync()).Where(c => !c.IsClosed).Cast<IListItem>(),
                    item => period = item as ListCollection);
                if (period == null)
                    return null;

                var commandes = await _getListCommandUseCase.ExecuteAsync(period.id);
                if (commandes.Count == 0)
                {
                    await _dialogService.ShowInfoAsync("Cette période n'a pas encore de commande.");
                    return null;
                }

                ListCommand? commande = null;
                await ShowPickerAsync(
                    () => Task.FromResult(commandes.Cast<IListItem>()),
                    item => commande = item as ListCommand);
                return commande?.id;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                await _dialogService.ShowErrorAsync(e);
                return null;
            }
        }

        private static async Task ShowPickerAsync(Func<Task<IEnumerable<IListItem>>> getItems, Action<IListItem> onPicked)
        {
            var mainPage = Easymakemoney.Utilities.AppPage.Current;
            if (mainPage != null)
            {
                var viewModel = new BottomSheetPopupListViewViewModel(getItemsFunc: getItems, onItemTappedAction: onPicked);
                await mainPage.ShowPopupAsync(new BottomSheetPopupListView(viewModel));
            }
        }

        [RelayCommand]
        private async Task Confirm()
        {
            if (IsBusy || _selected.Count == 0)
                return;

            var targetCommandId = CommandId;
            if (IsClassifying)
            {
                targetCommandId = await PickCommandeAsync() ?? 0;
                if (targetCommandId == 0)
                    return;
            }

            IsBusy = true;
            try
            {
                var count = _selected.Count;
                await _attachProductsUseCase.ExecuteAsync(targetCommandId, _selected.Values);
                await _dialogService.ShowInfoAsync(count > 1
                    ? $"{count} articles ajoutés à la commande."
                    : "Article ajouté à la commande.");

                if (IsClassifying)
                {
                    _selected.Clear();
                    SelectedCount = 0;
                    IsBusy = false;
                    await LoadFirstPageAsync();
                }
                else if (Shell.Current is { } shell)
                {
                    await shell.GoToAsync("..", true);
                }
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
    }
}
