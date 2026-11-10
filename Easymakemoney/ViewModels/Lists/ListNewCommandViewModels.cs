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
        IDialogService dialogService,
        ITripService tripService)
        {
            _tripService = tripService;
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

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanEdit))]
        private bool isClosed;

        public bool CanEdit => !IsClosed;
        public ICommand DeleteCommandCommand { get; }
        public async Task GetListCommandAsync()
        {
            // Création depuis Gestion : aucune liste à rafraîchir, le backend a classé la commande par sa date.
            if (CollectionId == 0)
            {
                await _dialogService.ShowInfoAsync("Commande enregistrée dans la période de sa date.");
                return;
            }

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
                await shell.GoToAsync($"{nameof(ListNewProductPage)}?CommandId={command.id}&Closed={IsClosed}", true);
        }

        private readonly ITripService _tripService;

        /// <summary>
        /// Rattache la commande à un voyage d'achat, ou la détache (choix du gérant : elle n'est plus déplacée par sa date).
        /// Ses achats apparaissent alors à part dans le suivi du voyage.
        /// </summary>
        [RelayCommand]
        private async Task ChooseTrip(ListCommand? command)
        {
            if (command == null || !CanEdit || Shell.Current is not { } shell)
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
            if (command.HasTrip)
                labels.Add(none);
            if (labels.Count == 0)
            {
                await _dialogService.ShowInfoAsync("Aucun voyage d'achat. Créez-en un dans Gestion › Voyages d'achat.");
                return;
            }

            var choice = await shell.DisplayActionSheetAsync("Voyage de cette commande", "Annuler", null, labels.ToArray());
            if (choice == null || choice == "Annuler")
                return;

            int? tripId = choice == none ? null : trips[labels.IndexOf(choice)].id;
            try
            {
                await _tripService.AttachCommandesAsync(tripId, new[] { command.id });
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                await _dialogService.ShowErrorAsync(ex);
                return;
            }

            await GetListCommandAsync();
        }

        /// <summary>
        /// Prix de vente des articles de la commande (coefficient sur le coût payé). Permis même en période fermée :
        /// le prix de vente n'est pas une donnée de période.
        /// </summary>
        [RelayCommand]
        private async Task ChangePrices(ListCommand? command)
        {
            if (command != null && Shell.Current is { } shell)
                await shell.GoToAsync($"{nameof(PriceChangePage)}?CommandeId={command.id}");
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
