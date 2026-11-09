using System.Windows.Input;
using Easymakemoney.Services.Dialog;

namespace Easymakemoney.ViewModels.Lists
{
    public partial class ListNewCommandViewModel : BaseViewModel
    {
        private readonly GetListCommandUseCase _getListCommandUseCase;

        private readonly DeleteCommandUseCase _deleteCommandUseCase;
        private readonly CommandFormModel _commandFormModel;

        private readonly SaveCommandUseCase _saveCommandUseCase;
        private readonly IDialogService _dialogService;

        public ObservableCollection<ListCommand> ListCommands { get; set; } = new ObservableCollection<ListCommand>();

        public ListNewCommandViewModel(GetListCommandUseCase getListCommandUseCase,SaveCommandUseCase saveCommandUseCase,
        DeleteCommandUseCase deleteCommandUseCase,
        CommandFormModel commandFormModel,
        IDialogService dialogService)
        {
            _saveCommandUseCase = saveCommandUseCase;
            _getListCommandUseCase = getListCommandUseCase;
            _commandFormModel = commandFormModel;
            _deleteCommandUseCase = deleteCommandUseCase;
            _dialogService = dialogService;
            DeleteCommandCommand = new AsyncRelayCommand<ListCommand>(DeleteCommand);
            ItemTappedProducts = new AsyncRelayCommand<ListCommand>(OnItemTapped);
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
                await _dialogService.ShowErrorAsync(ex);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task DeleteCommand(ListCommand? command)
        {
            if (command == null)
                return;

            bool confirmed = await _dialogService.ConfirmAsync(
                "Confirmation",
                "Êtes-vous sûr de vouloir supprimer cette commande ?",
                "Supprimer",
                "Annuler");
            if (!confirmed)
                return;

            try
            {
                var result = await _deleteCommandUseCase.ExecuteAsync(command.id);
                if (result)
                {
                    await GetListCommandAsync();
                    await _dialogService.ShowInfoAsync("Commande supprimée avec succès.");
                }
                else
                {
                    await _dialogService.ShowInfoAsync("Échec de la suppression de la commande.");
                }
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync(ex);
            }
        }

        private async Task OnItemTapped(ListCommand? command)
        {
            if (command == null)
                return;

            // Navigate to the ListNewCommandPage and pass the collection ID
            if (Shell.Current is { } shell)
                await shell.GoToAsync($"{nameof(ListNewProductPage)}?CommandId={command.id}", true);
        }


        [RelayCommand]
        public async Task ShowBottomSheet()
        {
            var viewModel = new BottomSheetPopupViewModel(
                popup: new Popup(),
                commandViewModel: this,
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
    }
}
