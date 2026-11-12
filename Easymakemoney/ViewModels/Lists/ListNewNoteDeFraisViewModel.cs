using System.Windows.Input;
using Easymakemoney.Services.Dialog;

namespace Easymakemoney.ViewModels.Lists
{
    public partial class ListNewNoteDeFraisViewModel : BaseViewModel
    {
        private readonly GetListNoteDeFraisUseCase _getListNoteDeFraisUseCase;
        private readonly DeleteNoteDeFraisUseCase _deleteNoteDeFraisUseCase;
        private readonly NoteDeFraisForModel _noteDeFraisForModel;

        private readonly GetListCollectionsUseCase _getListCollectionsUseCase;

        private readonly CollectionFormModel _collectionFormModel;

        private readonly ListNewCollectionViewModel _listNewCollectionViewModel;

        private readonly SaveCollectionUseCase _saveCollectionUseCase;

        private readonly SaveNoteDeFraisUseCase _saveNoteDeFraisUseCase;
        private readonly IDialogService _dialogService;

        public ListNewNoteDeFraisViewModel(GetListCollectionsUseCase getListCollectionsUseCase,
         GetListNoteDeFraisUseCase getListNoteDeFraisUseCase, DeleteNoteDeFraisUseCase deleteNoteDeFraisUseCase,
          SaveNoteDeFraisUseCase saveNoteDeFraisUseCase, NoteDeFraisForModel noteDeFraisForModel, SaveCollectionUseCase saveCollectionUseCase,
          ListNewCollectionViewModel listNewCollectionViewModel, CollectionFormModel collectionFormModel,
          Easymakemoney.UseCase.PeriodResultUseCase.PeriodUseCase periodUseCase,
          IDialogService dialogService,
          ITripService tripService)
        {
            _tripService = tripService;
            _periodUseCase = periodUseCase;
            _collectionFormModel = collectionFormModel;
            _listNewCollectionViewModel = listNewCollectionViewModel;
            _saveCollectionUseCase = saveCollectionUseCase;
            _getListCollectionsUseCase = getListCollectionsUseCase;
            _getListNoteDeFraisUseCase = getListNoteDeFraisUseCase;
            _deleteNoteDeFraisUseCase = deleteNoteDeFraisUseCase;
            _saveNoteDeFraisUseCase = saveNoteDeFraisUseCase;
            _noteDeFraisForModel = noteDeFraisForModel;
            _dialogService = dialogService;
            DeleteNoteDefraisCommand = new AsyncRelayCommand<NoteDeFrais>(DeleteNoteDeFrais);
            ItemTappedNoteDeFrais = new AsyncRelayCommand<NoteDeFrais>(OnItemTapped);

        }

        public ObservableCollection<NoteDeFrais> ListNoteDeFrais { get; set; } = new ObservableCollection<NoteDeFrais>();


        public ICommand ItemTappedNoteDeFrais { get; }
        public int CollectionId { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanEdit))]
        private bool isClosed;

        public bool CanEdit => !IsClosed;

        private readonly Easymakemoney.UseCase.PeriodResultUseCase.PeriodUseCase _periodUseCase;

        /// <summary>À l'ouverture de l'écran, sans choix préalable : la période en cours.</summary>
        public async Task SelectCurrentPeriodIfNoneAsync()
        {
            if (CollectionId != 0)
                return;

            try
            {
                var current = await _periodUseCase.GetCurrentPeriodAsync();
                if (current != null && CollectionId == 0)
                {
                    await SelectCollectionAsync(current);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        [RelayCommand]
        private async Task ReadReceipt()
        {
            if (Shell.Current is { } shell)
                await shell.GoToAsync($"{nameof(DocumentReadPage)}?Type=note", true);
        }

        /// <summary>Au retour sur l'écran : la liste peut avoir changé (lecture d'un reçu).</summary>
        public async Task RefreshAsync()
        {
            if (CollectionId == 0)
                await SelectCurrentPeriodIfNoneAsync();
            else
                await GetListNoteDeFraisAsync();
        }

        private async Task SelectCollectionAsync(ListCollection collection)
        {
            CollectionId = collection.id;
            IsClosed = collection.IsClosed;
            CollectionImage = collection.Photo;
            CollectionName = collection.Name;
            await GetListNoteDeFraisAsync();
        }
        public ICommand DeleteNoteDefraisCommand { get; }

        private string _collectionImage;
        public string CollectionImage
        {
            get => _collectionImage;
            set { _collectionImage = value; OnPropertyChanged(nameof(CollectionImage)); }
        }

        [ObservableProperty]
        private string? collectionName;

        public async Task GetListNoteDeFraisAsync()
        {
            if (IsBusy)
                return;

            IsBusy = true;
            ListNoteDeFrais.Clear();
            try
            {
                var noteDeFrais = await _getListNoteDeFraisUseCase.ExecuteAsync(CollectionId);

                foreach (var note in noteDeFrais)
                {
                    ListNoteDeFrais.Add(note);
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

        private readonly ITripService _tripService;

        /// <summary>
        /// Rattache la note à un voyage d'achat, ou la détache. Choix du gérant : la note n'est plus déplacée par sa date.
        /// </summary>
        [RelayCommand]
        private async Task ChooseTrip(NoteDeFrais? note)
        {
            if (note == null || !CanEdit || IsBusy)
                return;

            List<Trip> trips;
            try
            {
                trips = await _tripService.GetTripsAsync(1, 20);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                await _dialogService.ShowErrorAsync(ex);
                return;
            }

            const string none = "Aucun voyage";
            var labels = trips.Select(trip => $"{trip.Title} ({trip.DatesText})").ToList();
            if (note.HasTrip)
                labels.Add(none);
            if (labels.Count == 0)
            {
                await _dialogService.ShowInfoAsync("Aucun voyage d'achat. Créez-en un dans Gestion › Voyages d'achat.");
                return;
            }

            var choice = await _dialogService.ChooseAsync("Voyage de cette note", labels);
            if (choice == null)
                return;

            int? tripId = choice == none ? null : trips[labels.IndexOf(choice)].id;
            try
            {
                await _tripService.AttachNotesAsync(tripId, new[] { note.id });
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                await _dialogService.ShowErrorAsync(ex);
                return;
            }

            await GetListNoteDeFraisAsync();
        }

        private async Task DeleteNoteDeFrais(NoteDeFrais? noteDeFrais)
        {
            if (noteDeFrais == null)
                return;

            if (IsBusy)
                return;

            bool confirmed = await _dialogService.ConfirmAsync(
                "Confirmation",
                "Êtes-vous sûr de vouloir supprimer cette note de frais ?",
                "Supprimer",
                "Annuler");
            if (!confirmed)
                return;

            IsBusy = true;
            try
            {
                var result = await _deleteNoteDeFraisUseCase.ExecuteAsync(noteDeFrais.id);
                if (result)
                {
                    ListNoteDeFrais.Remove(noteDeFrais);
                    await _dialogService.ShowInfoAsync("Note de frais supprimée avec succès.");
                }
                else
                {
                    await _dialogService.ShowInfoAsync("Échec de la suppression de la note de frais.");
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

        private async Task OnItemTapped(NoteDeFrais? noteDeFrais)
        {
            if (noteDeFrais == null)
                return;
            await Task.CompletedTask;
        }


        [RelayCommand]
        public async Task ShowBottomSheetPopupListView()
        {
            var viewModel = new BottomSheetPopupListViewViewModel(
                getItemsFunc: async () => await _getListCollectionsUseCase.ExecuteAsync(),
                onItemTappedAction: async (item) =>
                {
                    var collection = item as ListCollection;
                    if (collection != null)
                    {
                        await SelectCollectionAsync(collection);
                    }
                });

            var popup = new BottomSheetPopupListView(viewModel);
            if (Easymakemoney.Utilities.AppPage.Current != null)
                await Easymakemoney.Utilities.AppPage.Current!.ShowPopupAsync(popup);
        }

        [RelayCommand]
        public async Task ShowBottomSheet()
        {
            if (CollectionId == 0)
            {
                await _dialogService.ShowInfoAsync("Vous devez choisir obligatoirement une période");
                return;
            }
            var viewModel = new BottomSheetPopupViewModel(
                popup: new Popup(),
                noteDeFraisViewModel: this,
                isNoteDeFraisForm: true,
                saveNoteDeFraisUseCase: _saveNoteDeFraisUseCase,
                noteDeFraisForm: _noteDeFraisForModel,
                dialogService: _dialogService);

            var popup = new BottomSheetPopup(viewModel);
            var mainPage = Easymakemoney.Utilities.AppPage.Current;

            if (mainPage != null)
            {
                await mainPage.ShowPopupAsync(popup);
            }
        }

        [RelayCommand]
        public async Task ShowBottomSheetCreateList()
        {
            try
            {
                var viewModel = new BottomSheetPopupViewModel(
                    popup: new Popup(),
                    saveCollectionUseCase: _saveCollectionUseCase,
                    isCollectionForm: true,
                    collectionViewModel: _listNewCollectionViewModel,
                    collectionFormModel: _collectionFormModel,
                    dialogService: _dialogService);

                var popup = new BottomSheetPopup(viewModel);
                var mainPage = Easymakemoney.Utilities.AppPage.Current;
                if (mainPage != null)
                {
                    Console.WriteLine("AShowPopupAsync");
                    await mainPage.ShowPopupAsync(popup);
                }
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync(ex);
                Console.WriteLine($"Exception: {ex}");
            }
        }




    }


}