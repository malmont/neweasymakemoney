using System.Windows.Input;
using Easymakemoney.ViewModels.FormModels;

namespace Easymakemoney.ViewModels
{
    public partial class BottomSheetPopupViewModel : ObservableObject
    {
        private readonly CreateCollectionUseCase _createCollectionUseCase;
        private readonly CreateCommandUseCase _createCommandUseCase;
        private readonly IPreferenceService _preferenceService;
        private Popup _popup;
        private readonly ListNewCollectionViewModel _collectionViewModel;
        private readonly ListNewCommandViewModel _commandViewModel;
        
        public BottomSheetPopupViewModel(CreateCollectionUseCase createCollectionUseCase, CreateCommandUseCase createCommandUseCase, IPreferenceService preferenceService, Popup popup, ListNewCollectionViewModel collectionViewModel, ListNewCommandViewModel commandViewModel, bool isCollectionForm, CollectionFormModel collectionFormModel, CommandFormModel commandFormModel)
        {
            _createCollectionUseCase = createCollectionUseCase;
            _createCommandUseCase = createCommandUseCase;
            _preferenceService = preferenceService;
            _popup = popup;
            _collectionViewModel = collectionViewModel;
            _commandViewModel = commandViewModel;
            IsCollectionForm = isCollectionForm;
            IsCommandForm = !isCollectionForm;
            CollectionForm = collectionFormModel;
            CommandForm = commandFormModel;

            SaveCommand = new RelayCommand(Save);
            CancelCommand = new RelayCommand(Cancel);
        }

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        private bool _isCollectionForm;
        public bool IsCollectionForm
        {
            get => _isCollectionForm;
            set => SetProperty(ref _isCollectionForm, value);
        }

        private bool _isCommandForm;
        public bool IsCommandForm
        {
            get => _isCommandForm;
            set => SetProperty(ref _isCommandForm, value);
        }

        public CollectionFormModel CollectionForm { get; }
        public CommandFormModel CommandForm { get; }

        public void SetPopupInstance(Popup popup) // Ajouter cette méthode pour recevoir l'instance du popup
        {
            _popup = popup;
        }

        private async void Save()
        {
            try
            {
                if (IsCollectionForm)
                {
                    var user = _preferenceService.GetUserId();
                    var newCollection = new ListCollection
                    {
                        budgetCollection = CollectionForm.BudgetCollection,
                        startDateCollection = CollectionForm.StartDateCollection.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                        endDateCollection = CollectionForm.EndDateCollection.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                        del = CollectionForm.Del,
                        nomCollection = CollectionForm.NomCollection,
                        photoCollection = CollectionForm.PhotoCollection,
                        userId = user
                    };
                    var result = await _createCollectionUseCase.ExecuteAsync(newCollection);
                    if (result)
                    {
                        await Application.Current.MainPage.DisplayAlert("Success", "Collection saved", "OK");
                        await _collectionViewModel.GetListCollectionAsync();
                        _popup.Close();
                    }
                    else
                    {
                        await Application.Current.MainPage.DisplayAlert("Error", "Failed to save collection", "OK");
                    }
                }
                else
                {
                    var newCommand = new ListCommand
                    {
                        budget = CommandForm.Budget,
                        date = CommandForm.Date.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                        name = CommandForm.Name,
                        photo = CommandForm.Photo,
                        collectionId = _commandViewModel.CollectionId
                    };
                    var result = await _createCommandUseCase.ExecuteAsync(newCommand,_commandViewModel.CollectionId);
                    if (result)
                    {
                        await Application.Current.MainPage.DisplayAlert("Success", "Command saved", "OK");
                        await _commandViewModel.GetListCommandAsync();
                        _popup.Close();
                    }
                    else
                    {
                        await Application.Current.MainPage.DisplayAlert("Error", "Failed to save command", "OK");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                await Application.Current.MainPage.DisplayAlert("Error", "Failed to save", "OK");
            }
        }

        private void Cancel()
        {
            _popup.Close();
        }
    }
}
