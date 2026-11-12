
namespace Easymakemoney.ViewModels.Lists
{
    public partial class AllListsViewModel
	{
        /// <summary>Navigation (point d'entrée des tests).</summary>
        public Func<string, Task> NavigateAsync { get; set; } = route => Shell.Current?.GoToAsync(route) ?? Task.CompletedTask;

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
            var mainPage = Easymakemoney.Utilities.AppPage.Current;
            if (mainPage != null)
            {
                await mainPage.ShowPopupAsync(popup);
            }
        }

        [RelayCommand]
        private async Task NavigateToSupplierInvoice()
        {
            await NavigateAsync($"{nameof(DocumentReadPage)}?Type=facture");
        }

        [RelayCommand]
        private async Task NavigateToToClassify()
        {
            await NavigateAsync(nameof(AttachProductsPage));
        }

        [RelayCommand]
        private async Task NavigateToCollection()
        {
            await NavigateAsync(nameof(ListNewCollectionPage));
        }

        [RelayCommand]
        private async Task NavigateToNoteDeFrais()
        {
            await NavigateAsync(nameof(ListNewNoteDeFraisPage));
        }

        [RelayCommand]
        private async Task NavigateToNoteFournisseur()
        {
            await NavigateAsync(nameof(ListNewFournisseurPage));
        }

        [RelayCommand]
        private async Task NavigateToFraisDePort()
        {
            await NavigateAsync(nameof(ListNewFraisDePortPage));
        }

        [RelayCommand]
        private async Task NavigateToAdvertising()
        {
            await NavigateAsync(nameof(CampaignListPage));
        }

        [RelayCommand]
        private async Task NavigateToTrips()
        {
            await NavigateAsync(nameof(TripListPage));
        }

        [RelayCommand]
        private async Task NavigateToToReceive()
        {
            await NavigateAsync(nameof(ToReceivePage));
        }

        [RelayCommand]
        private async Task NavigateToPreparation()
        {
            await NavigateAsync(nameof(PreparationPage));
        }

        [RelayCommand]
        private async Task NavigateToPromotions()
        {
            await NavigateAsync(nameof(PromotionListPage));
        }

        [RelayCommand]
        private async Task NavigateToVariantPromotions()
        {
            await NavigateAsync(nameof(VariantPromotionListPage));
        }

        [RelayCommand]
        private async Task NavigateToPriceBatches()
        {
            await NavigateAsync(nameof(PriceBatchListPage));
        }

        [RelayCommand]
        private async Task NavigateToFixedCosts()
        {
            await NavigateAsync(nameof(FixedCostsPage));
        }

        [RelayCommand]
        private async Task NavigateToFiscalYear()
        {
            await NavigateAsync(nameof(FiscalYearPage));
        }

        [RelayCommand]
        private async Task NavigateToTreasury()
        {
            await NavigateAsync(nameof(TreasuryPage));
        }

        [RelayCommand]
        private async Task NavigateToSupport()
        {
            await NavigateAsync(nameof(SupportTicketListPage));
        }

        [RelayCommand]
        private async Task NavigateToReadiness()
        {
            await NavigateAsync(nameof(ManagementReadinessPage));
        }

        [RelayCommand]
        private async Task NavigateToReprise()
        {
            await NavigateAsync(nameof(ReprisePage));
        }
    }
}

