
using Easymakemoney.Services.Dialog;

namespace Easymakemoney.ViewModels.Dashboard
{
    public partial class DashBoardCommandViewModel : BaseViewModel
    {

        private readonly ListNewCommandViewModel _listNewCommandViewModel;

        private readonly SaveCommandUseCase _saveCommandUseCase;

        private readonly GetListCommandUseCase _getListCommandUseCase;
        private readonly CommandFormModel _commandFormModel;

        private readonly GetDashboardCommandUseCase _getDashboardCommandUseCase;
        private readonly IDialogService _dialogService;

        public DashBoardCommandViewModel(GetDashboardCommandUseCase getDashboardCommandUseCase, ListNewCommandViewModel listNewCommandViewModel, CommandFormModel commandFormModel,
            SaveCommandUseCase saveCommandUseCase, GetListCommandUseCase getListCommandUseCase, IDialogService dialogService)
        {
            _commandFormModel = commandFormModel;
            _getListCommandUseCase = getListCommandUseCase;
            _saveCommandUseCase = saveCommandUseCase;
            _listNewCommandViewModel = listNewCommandViewModel;
            _getDashboardCommandUseCase = getDashboardCommandUseCase;
            _dialogService = dialogService;
        }

        public DashboardCommand _dashBoardCommand;
        public DashboardCommand DashBoardCommand
        {
            get => _dashBoardCommand;
            set { _dashBoardCommand = value; OnPropertyChanged(nameof(DashBoardCommand)); }
        }

        public int CommandId { get; set; }

        public int CollectionId { get; set; }

        private string _commandImage;

        public string CommandImage
        {
            get => _commandImage;
            set { _commandImage = value; OnPropertyChanged(nameof(CommandImage)); }
        }

        [ObservableProperty]
        private string? commandName;


        public async Task GetDashboardCommandAsync(int CommandId)
        {
            if (IsBusy)
                return;

            IsBusy = true;
            try
            {
                DashBoardCommand = await _getDashboardCommandUseCase.GetDashboardCommand(CommandId);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                await _dialogService.ShowErrorAsync(ex);
            }
            finally
            {
                IsBusy = false;
            }
        }
        [RelayCommand]
        public async Task ShowBottomSheetCreateCommand()
        {
            if (CollectionId == 0)
            {
                await _dialogService.ShowInfoAsync("Vous devez choisir obligatoirement une période");
                return;
            }
            _listNewCommandViewModel.CollectionId = CollectionId;
            var viewModel = new BottomSheetPopupViewModel(
                popup: new Popup(),
                commandViewModel: _listNewCommandViewModel,
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
        public async Task ShowBottomSheetPopupListCommandView()
        {
            if (CollectionId == 0)
            {
                await _dialogService.ShowInfoAsync("Vous devez choisir obligatoirement une période");
                return;
            }
            var viewModel = new BottomSheetPopupListViewViewModel(
                getItemsFunc: async () => await _getListCommandUseCase.ExecuteAsync(CollectionId),
                onItemTappedAction: async (item) =>
                {
                    var command = item as ListCommand;
                    if (command != null)
                    {
                        this.CommandId = command.id;
                        this.CommandImage = command.Photo;
                        this.CommandName = command.Name;
                        await this.GetDashboardCommandAsync(command.id);
                    }
                });

            var popup = new BottomSheetPopupListView(viewModel);
            if (Application.Current?.MainPage != null)
                await Application.Current.MainPage.ShowPopupAsync(popup);
        }

        [RelayCommand]
        private async Task ItemTapped()
        {
            if (CommandId == 0)
                return;

            // Navigate to the ListNewCommandPage and pass the collection ID
            if (Shell.Current is { } shell)
                await shell.GoToAsync($"{nameof(DashBoardProductPage)}?CommandId={CommandId}", true);
        }

    }

}