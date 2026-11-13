// Code antérieur aux annotations de nullité : avertissements coupés ici, sans effet sur l'exécution.
#nullable disable warnings

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
            var mainPage = Easymakemoney.Utilities.AppPage.Current;

            if (mainPage != null)
            {
                await mainPage.ShowPopupAsync(popup);
            }
        }
        /// <summary>Commandes de la période ; liste vide et message en cas d'erreur.</summary>
        public async Task<ObservableCollection<ListCommand>> LoadCommandsAsync()
        {
            try
            {
                return await _getListCommandUseCase.ExecuteAsync(CollectionId);
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync(ex);
                return new ObservableCollection<ListCommand>();
            }
        }

        public async Task SelectCommandAsync(ListCommand command)
        {
            CommandId = command.id;
            CommandImage = command.Photo;
            CommandName = command.Name;
            await GetDashboardCommandAsync(command.id);
        }

        /// <summary>Navigation (point d'entrée des tests).</summary>
        public Func<string, Task> NavigateAsync { get; set; } = route => Shell.Current?.GoToAsync(route) ?? Task.CompletedTask;


        [RelayCommand]
        public async Task ShowBottomSheetPopupListCommandView()
        {
            if (CollectionId == 0)
            {
                await _dialogService.ShowInfoAsync("Vous devez choisir obligatoirement une période");
                return;
            }
            var viewModel = new BottomSheetPopupListViewViewModel(
                getItemsFunc: async () => await LoadCommandsAsync(),
                onItemTappedAction: async item =>
                {
                    if (item is ListCommand command)
                        await SelectCommandAsync(command);
                });

            var popup = new BottomSheetPopupListView(viewModel);
            if (Easymakemoney.Utilities.AppPage.Current != null)
                await Easymakemoney.Utilities.AppPage.Current!.ShowPopupAsync(popup);
        }

        [RelayCommand]
        private async Task ItemTapped()
        {
            if (CommandId == 0)
                return;

            // Navigate to the ListNewCommandPage and pass the collection ID
            await NavigateAsync($"{nameof(DashBoardProductPage)}?CommandId={CommandId}");
        }

    }

}