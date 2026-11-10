
namespace Easymakemoney.ViewModels.Lists
{
    public partial class AllListsViewModel
	{
		private readonly ListNewCommandViewModel _commandViewModel;
		private readonly SaveCommandUseCase _saveCommandUseCase;
		private readonly CommandFormModel _commandFormModel;
		private readonly Easymakemoney.Services.Dialog.IDialogService _dialogService;

		public AllListsViewModel(ListNewCommandViewModel commandViewModel, SaveCommandUseCase saveCommandUseCase,
			CommandFormModel commandFormModel, Easymakemoney.Services.Dialog.IDialogService dialogService)
		{
			_commandViewModel = commandViewModel;
			_saveCommandUseCase = saveCommandUseCase;
			_commandFormModel = commandFormModel;
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
                commandFormModel: _commandFormModel,
                dialogService: _dialogService);

            var popup = new BottomSheetPopup(viewModel);
            var mainPage = Application.Current?.MainPage;
            if (mainPage != null)
            {
                await mainPage.ShowPopupAsync(popup);
            }
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
        private async Task NavigateToReprise()
        {
            if (Shell.Current is { } shell)
                await shell.GoToAsync(nameof(ReprisePage));
        }
    }
}

