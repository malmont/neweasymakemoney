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
        private readonly IDialogService _dialogService;

        // La sélection survit à un changement de recherche.
        private readonly Dictionary<int, AvailableProduct> _selected = new();
        private CancellationTokenSource? _searchDelay;
        private int _loadVersion;
        private int _page;
        private int _total;
        private bool _isLoadingMore;

        public AttachProductsViewModel(GetAvailableProductsUseCase getAvailableProductsUseCase,
            AttachProductsUseCase attachProductsUseCase, IDialogService dialogService)
        {
            _getAvailableProductsUseCase = getAvailableProductsUseCase;
            _attachProductsUseCase = attachProductsUseCase;
            _dialogService = dialogService;
        }

        public ObservableCollection<AvailableProduct> Products { get; } = new ObservableCollection<AvailableProduct>();

        public int CommandId { get; set; }

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
            ? $"Ajouter {SelectedCount} articles"
            : "Ajouter 1 article";

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
                var result = await _getAvailableProductsUseCase.ExecuteAsync(SearchText, 1, PageSize);
                if (version != _loadVersion)
                    return;

                Products.Clear();
                AddResults(result);
                _page = 1;
                EmptyTitle = "Aucun article disponible";
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
                var result = await _getAvailableProductsUseCase.ExecuteAsync(SearchText, _page + 1, PageSize);
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
            var page = Application.Current?.MainPage;
            if (product == null || page == null)
                return;

            var text = await page.DisplayPromptAsync(
                "Coût d'achat",
                product.name,
                "Valider",
                "Annuler",
                keyboard: Keyboard.Numeric);

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

        [RelayCommand]
        private async Task Confirm()
        {
            if (IsBusy || _selected.Count == 0)
                return;

            IsBusy = true;
            try
            {
                var count = _selected.Count;
                await _attachProductsUseCase.ExecuteAsync(CommandId, _selected.Values);
                await _dialogService.ShowInfoAsync(count > 1
                    ? $"{count} articles ajoutés à la commande."
                    : "Article ajouté à la commande.");

                if (Shell.Current is { } shell)
                    await shell.GoToAsync("..", true);
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
