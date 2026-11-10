
namespace Easymakemoney.ViewModels.Lists
{
    public partial class AllListsViewModel
	{
		private readonly ListNewCommandViewModel _commandViewModel;
		private readonly SaveCommandUseCase _saveCommandUseCase;
		private readonly IServiceProvider _services;
		private readonly Easymakemoney.Services.Dialog.IDialogService _dialogService;

		public AllListsViewModel(ListNewCommandViewModel commandViewModel, SaveCommandUseCase saveCommandUseCase,
			IServiceProvider services, Easymakemoney.Services.Dialog.IDialogService dialogService)
		{
			_commandViewModel = commandViewModel;
			_saveCommandUseCase = saveCommandUseCase;
			_services = services;
			_dialogService = dialogService;
        }

        [RelayCommand]
        private async Task NewCommand()
        {
            _commandViewModel.CollectionId = 0;
            var viewModel = new BottomSheetPopupViewModel(
                popup: new Popup(),
                commandViewModel: _commandViewModel,
                isCommandForm: true,
                saveCommandUseCase: _saveCommandUseCase,
                // Formulaire neuf : la liste des fournisseurs est relue, et rien ne reste de la saisie précédente.
                commandFormModel: Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<CommandFormModel>(_services),
                dialogService: _dialogService);

            var popup = new BottomSheetPopup(viewModel);
            var mainPage = Application.Current?.MainPage;
            if (mainPage != null)
            {
                await mainPage.ShowPopupAsync(popup);
            }
        }

        [RelayCommand]
        private async Task NavigateToSupplierInvoice()
        {
            if (Shell.Current is { } shell)
                await shell.GoToAsync($"{nameof(DocumentReadPage)}?Type=facture");
        }

        [RelayCommand]
        private async Task NavigateToToClassify()
        {
            if (Shell.Current is { } shell)
                await shell.GoToAsync(nameof(AttachProductsPage));
        }

        [RelayCommand]
        private async Task NavigateToCollection()
        {
            if (Shell.Current is { } shell)
                await shell.GoToAsync(nameof(ListNewCollectionPage));
        }

        [RelayCommand]
        private async Task NavigateToNoteDeFrais()
        {
            if (Shell.Current is { } shell)
                await shell.GoToAsync(nameof(ListNewNoteDeFraisPage));
        }

        [RelayCommand]
        private async Task NavigateToNoteFournisseur()
        {
            if (Shell.Current is { } shell)
                await shell.GoToAsync(nameof(ListNewFournisseurPage));
        }

        [RelayCommand]
        private async Task NavigateToFraisDePort()
        {
            if (Shell.Current is { } shell)
                await shell.GoToAsync(nameof(ListNewFraisDePortPage));
        }

        [RelayCommand]
        private async Task NavigateToAdvertising()
        {
            if (Shell.Current is { } shell)
                await shell.GoToAsync(nameof(CampaignListPage));
        }

        [RelayCommand]
        private async Task NavigateToTrips()
        {
            if (Shell.Current is { } shell)
                await shell.GoToAsync(nameof(TripListPage));
        }

        [RelayCommand]
        private async Task NavigateToTreasury()
        {
            if (Shell.Current is { } shell)
                await shell.GoToAsync(nameof(TreasuryPage));
        }

        [RelayCommand]
        private async Task NavigateToReprise()
        {
            if (Shell.Current is { } shell)
                await shell.GoToAsync(nameof(ReprisePage));
        }
    }
}

