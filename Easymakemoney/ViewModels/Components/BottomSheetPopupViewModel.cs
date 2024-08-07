using System.Windows.Input;
namespace Easymakemoney.ViewModels
{
    public partial class BottomSheetPopupViewModel : ObservableObject
    {
        private readonly CreateCollectionUseCase _createCollectionUseCase;
        private readonly IPreferenceService _preferenceService;
        private readonly Popup _popup;
        private readonly ListNewCollectionViewModel _parentViewModel;

        public BottomSheetPopupViewModel(CreateCollectionUseCase createCollectionUseCase, IPreferenceService preferenceService, Popup popup, ListNewCollectionViewModel parentViewModel)
        {
            _createCollectionUseCase = createCollectionUseCase;
            _preferenceService = preferenceService;
            _popup = popup;
            _parentViewModel = parentViewModel;

            SaveCommand = new RelayCommand(SaveNewCollection);
            CancelCommand = new RelayCommand(CancelPopup);

            BudgetCollection = 0.0f; // Initialisation par défaut
            StartDateCollection = DateTime.Now;
            EndDateCollection = DateTime.Now;
            Del = false; // Initialisation par défaut
            NomCollection = string.Empty; // Initialisation par défaut
            PhotoCollection = string.Empty; // Initialisation par défaut
        }

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        private float _budgetCollection;
        public float BudgetCollection
        {
            get => _budgetCollection;
            set { _budgetCollection = value; OnPropertyChanged(nameof(BudgetCollection)); }
        }

        private DateTime _startDateCollection;
        public DateTime StartDateCollection
        {
            get => _startDateCollection;
            set { _startDateCollection = value; OnPropertyChanged(nameof(StartDateCollection)); }
        }

        private DateTime _endDateCollection;
        public DateTime EndDateCollection
        {
            get => _endDateCollection;
            set { _endDateCollection = value; OnPropertyChanged(nameof(EndDateCollection)); }
        }

        private bool _del;
        public bool Del
        {
            get => _del;
            set { _del = value; OnPropertyChanged(nameof(Del)); }
        }

        private string _nomCollection;
        public string NomCollection
        {
            get => _nomCollection;
            set { _nomCollection = value; OnPropertyChanged(nameof(NomCollection)); }
        }

        private string _photoCollection;
        public string PhotoCollection
        {
            get => _photoCollection;
            set { _photoCollection = value; OnPropertyChanged(nameof(PhotoCollection)); }
        }

        private async void SaveNewCollection()
        {
            try
            {
                var user = _preferenceService.GetUserId(); // Obtenir les informations de l'utilisateur

                var newCollection = new ListCollection
                {
                    budgetCollection = BudgetCollection,
                    startDateCollection = StartDateCollection.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    endDateCollection = EndDateCollection.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    del = Del,
                    nomCollection = NomCollection,
                    photoCollection = PhotoCollection,
                    userId = user
                };

                Debug.WriteLine($"Sending new collection data: {JsonConvert.SerializeObject(newCollection)}");

                var result = await _createCollectionUseCase.ExecuteAsync(newCollection);

                if (result)
                {
                    await Application.Current.MainPage.DisplayAlert("Success", "Collection saved", "OK");
                    await _parentViewModel.GetListCollectionAsync(); // Mettre à jour la liste
                    _popup.Close(); // Fermer la popup en cas de succès
                }
                else
                {
                    await Application.Current.MainPage.DisplayAlert("Error", "Failed to save collection", "OK");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                await Application.Current.MainPage.DisplayAlert("Error", "Failed to save collection", "OK");
            }
        }

        private void CancelPopup()
        {
            _popup.Close(); // Fermer la popup en cas d'annulation
        }
    }
}
