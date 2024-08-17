using System.Windows.Input;

namespace Easymakemoney.ViewModels.Lists
{
    public partial class ListNewCommandViewModel : BaseViewModel
    {
        private readonly GetListCommandUseCase _getListCommandUseCase;

        private readonly DeleteCommandUseCase _deleteCommandUseCase;
        private readonly CommandFormModel _commandFormModel;

        private readonly SaveCommandUseCase _saveCommandUseCase;

        public ObservableCollection<ListCommand> ListCommands { get; set; } = new ObservableCollection<ListCommand>();

        public ListNewCommandViewModel(GetListCommandUseCase getListCommandUseCase,SaveCommandUseCase saveCommandUseCase,
        DeleteCommandUseCase deleteCommandUseCase,
        CommandFormModel commandFormModel)
        {
            _saveCommandUseCase = saveCommandUseCase;
            _getListCommandUseCase = getListCommandUseCase;
            _commandFormModel = commandFormModel;
            _deleteCommandUseCase = deleteCommandUseCase;
            DeleteCommandCommand = new RelayCommand<ListCommand>(DeleteCommand);
            ItemTappedProducts = new RelayCommand<ListCommand>(OnItemTapped);
        }

        public ICommand ItemTappedProducts { get; }
        public int CollectionId { get; set; }
        public ICommand DeleteCommandCommand { get; }
        public async Task GetListCommandAsync()
        {
            if (IsBusy)
                return;

            IsBusy = true;
            ListCommands.Clear();
            try
            {
                var commands = await _getListCommandUseCase.ExecuteAsync(CollectionId);

                foreach (var command in commands)
                {
                    ListCommands.Add(command);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                await Shell.Current.DisplayAlert("Error", "Unable to get commands", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async void DeleteCommand(ListCommand command)
        {
            if (command == null)
                return;

            var result = await _deleteCommandUseCase.ExecuteAsync(command.id); ;
            if (result)
            {
                await GetListCommandAsync();
                await Shell.Current.DisplayAlert("Success", "Collection deleted", "OK");
            }
            else
            {
                await Shell.Current.DisplayAlert("Error", "Failed to delete collection", "OK");
            }
        }

        private async void OnItemTapped(ListCommand command)
        {
            if (command == null)
                return;

            // Navigate to the ListNewCommandPage and pass the collection ID
            await Shell.Current.GoToAsync($"{nameof(ListNewProductPage)}?CommandId={command.id}", true);
        }


        [ICommand]
        public async Task ShowBottomSheet()
        {
            var viewModel = new BottomSheetPopupViewModel(
            popup: new Popup(),
            commandViewModel: this,
            isCommandForm: true,
            saveCommandUseCase: _saveCommandUseCase,
            commandFormModel: _commandFormModel);

            var popup = new BottomSheetPopup(viewModel);
            var mainPage = Application.Current?.MainPage;

            if (mainPage != null)
            {
                await mainPage.ShowPopupAsync(popup);
            }
        }
    }
}
