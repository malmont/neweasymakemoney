using System.Globalization;
using Easymakemoney.Services.Dialog;
using Newtonsoft.Json.Linq;

namespace Easymakemoney.ViewModels.Lists
{
    /// <summary>
    /// Lot EDIT : modifier le frais de port d'une commande (PATCH /api/commandes/{id}/frais-de-port). Prix dans la devise de la commande,
    /// en cents. Seuls les champs changés partent. Période ou commande fermée : refus du serveur (PERIOD_CLOSED, COMMANDE_CLOSED) montré tel quel.
    /// </summary>
    public partial class ShippingEditViewModel : BaseViewModel, IQueryAttributable
    {
        private readonly IManagementEditService _editService;
        private readonly GetListTransporteurUseCase _getTransporteursUseCase;
        private readonly IDialogService _dialogService;

        private JObject _initial = new();
        private int? _initialTransporteurId;

        public ShippingEditViewModel(IManagementEditService editService, GetListTransporteurUseCase getTransporteursUseCase, IDialogService dialogService)
        {
            _editService = editService;
            _getTransporteursUseCase = getTransporteursUseCase;
            _dialogService = dialogService;
        }

        public int CommandeId { get; private set; }

        public ObservableCollection<Transporteur> Transporteurs { get; } = new();

        [ObservableProperty]
        private string commandeName = string.Empty;

        [ObservableProperty]
        private string priceLabel = "Prix";

        [ObservableProperty]
        private string name = string.Empty;

        [ObservableProperty]
        private string? priceText;

        [ObservableProperty]
        private string? facture;

        [ObservableProperty]
        private string? tracknumber;

        [ObservableProperty]
        private Transporteur? selectedTransporteur;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasError))]
        private string? errorText;

        public bool HasError => !string.IsNullOrWhiteSpace(ErrorText);

        public Func<Task> GoBackAsync { get; set; } = () => Shell.Current?.GoToAsync("..") ?? Task.CompletedTask;

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.TryGetValue("Row", out var value) && value is FraisDePortCommandeRow row)
                Load(row);
        }

        public void Load(FraisDePortCommandeRow row)
        {
            CommandeId = row.Commande.id;
            CommandeName = row.Commande.Name;
            var frais = row.Frais;
            var currency = frais?.currency ?? row.Commande.currency;
            PriceLabel = string.IsNullOrWhiteSpace(currency) ? "Prix" : $"Prix ({currency})";
            Name = frais?.name ?? string.Empty;
            PriceText = frais?.price is long cents ? (cents / 100m).ToString("0.00", CultureInfo.CurrentCulture) : null;
            Facture = frais?.facture;
            Tracknumber = frais?.tracknumber;
            _initialTransporteurId = frais?.transporteur?.id;
            SelectedTransporteur = Transporteurs.FirstOrDefault(t => t.id == _initialTransporteurId);
            ErrorText = null;
            _initial = Current(frais?.price);
        }

        public async Task LoadTransporteursAsync()
        {
            try
            {
                var transporteurs = await _getTransporteursUseCase.ExecuteAsync();
                Transporteurs.Clear();
                foreach (var transporteur in transporteurs)
                    Transporteurs.Add(transporteur);
                SelectedTransporteur = Transporteurs.FirstOrDefault(t => t.id == _initialTransporteurId);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ShippingEdit] Transporteurs illisibles : {ex.Message}");
            }
        }

        private JObject Current(long? price) => new()
        {
            ["name"] = EditPayload.Text(Name),
            ["price"] = price,
            ["facture"] = EditPayload.Text(Facture),
            ["tracknumber"] = EditPayload.Text(Tracknumber),
            // Le transporteur ne se vide pas : sans choix, celui d'avant reste.
            ["transporteur"] = SelectedTransporteur?.id ?? _initialTransporteurId,
        };

        /// <summary>Ce qui partira au serveur ; null si le prix est illisible.</summary>
        public JObject? Changes()
        {
            var price = InvoiceReview.ParseCents(PriceText);
            if (price == null)
                return null;
            var changes = EditPayload.Changes(_initial, Current(price));
            if (changes["transporteur"] is JValue { Type: JTokenType.Integer } id)
                changes["transporteur"] = new JObject { ["id"] = id };
            return changes;
        }

        [RelayCommand]
        private async Task Save()
        {
            if (IsBusy)
                return;

            ErrorText = null;
            if (EditPayload.Text(Name) == null)
            {
                ErrorText = "Le nom du frais de port est obligatoire.";
                return;
            }

            var changes = Changes();
            if (changes == null)
            {
                ErrorText = "Prix invalide : saisissez un montant, par exemple 24,90.";
                return;
            }
            if (!changes.HasValues)
            {
                await _dialogService.ShowInfoAsync("Aucun changement à enregistrer.");
                return;
            }

            IsBusy = true;
            try
            {
                await _editService.UpdateFraisDePortAsync(CommandeId, changes);
                await _dialogService.ShowInfoAsync("Frais de port modifié.");
                await GoBackAsync();
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
    }
}
