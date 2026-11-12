using Easymakemoney.Services.Api;
using Easymakemoney.Services.Dialog;
using Newtonsoft.Json.Linq;

namespace Easymakemoney.ViewModels.Lists
{
    /// <summary>
    /// Lot EDIT : modifier un fournisseur (PATCH /api/fournisseurs/{id}). Seuls les champs changés partent. La devise d'achat se fige
    /// dès la première commande : le serveur refuse (409 FOURNISSEUR_CURRENCY_LOCKED) et l'écran le dit sans fermer.
    /// </summary>
    public partial class SupplierEditViewModel : BaseViewModel, IQueryAttributable
    {
        public const string SiteCurrencyChoice = "Devise du site";

        private readonly IManagementEditService _editService;
        private readonly GetListTypeFournisseurUseCase _getTypesUseCase;
        private readonly IDialogService _dialogService;

        private JObject _initial = new();
        private int? _initialTypeId;
        private string _initialCurrencyChoice = SiteCurrencyChoice;

        public SupplierEditViewModel(IManagementEditService editService, GetListTypeFournisseurUseCase getTypesUseCase, IDialogService dialogService)
        {
            _editService = editService;
            _getTypesUseCase = getTypesUseCase;
            _dialogService = dialogService;
        }

        public int SupplierId { get; private set; }

        public ObservableCollection<TypeFournisseur> Types { get; } = new();

        public ObservableCollection<string> CurrencyChoices { get; } = new() { SiteCurrencyChoice, "CAD", "EUR", "USD" };

        [ObservableProperty]
        private string name = string.Empty;

        [ObservableProperty]
        private string? adresse;

        [ObservableProperty]
        private string? ville;

        [ObservableProperty]
        private string? pays;

        [ObservableProperty]
        private string? tel;

        [ObservableProperty]
        private TypeFournisseur? selectedType;

        [ObservableProperty]
        private string selectedCurrency = SiteCurrencyChoice;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasError))]
        private string? errorText;

        public bool HasError => !string.IsNullOrWhiteSpace(ErrorText);

        /// <summary>Retour à la liste après l'enregistrement (point d'entrée des tests).</summary>
        public Func<Task> GoBackAsync { get; set; } = () => Shell.Current?.GoToAsync("..") ?? Task.CompletedTask;

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.TryGetValue("Fournisseur", out var value) && value is ListFournisseur fournisseur)
                Load(fournisseur);
        }

        public void Load(ListFournisseur fournisseur)
        {
            SupplierId = fournisseur.id;
            Name = fournisseur.name;
            Adresse = fournisseur.adresse;
            Ville = fournisseur.ville;
            Pays = fournisseur.pays;
            Tel = fournisseur.tel;
            _initialTypeId = fournisseur.typeFournisseurId;
            SelectedType = Types.FirstOrDefault(type => type.id == _initialTypeId);

            var code = EditPayload.Text(fournisseur.purchaseCurrency)?.ToUpperInvariant();
            if (code != null && !CurrencyChoices.Contains(code))
                CurrencyChoices.Add(code);
            _initialCurrencyChoice = code ?? SiteCurrencyChoice;
            SelectedCurrency = _initialCurrencyChoice;
            ErrorText = null;
            _initial = Current();
        }

        /// <summary>Types de fournisseur, puis sélection du type actuel.</summary>
        public async Task LoadTypesAsync()
        {
            try
            {
                var types = await _getTypesUseCase.ExecuteAsync();
                Types.Clear();
                foreach (var type in types)
                    Types.Add(type);
                SelectedType = Types.FirstOrDefault(type => type.id == _initialTypeId);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SupplierEdit] Types illisibles : {ex.Message}");
            }
        }

        /// <summary>État du formulaire, au format du PATCH. Sans type choisi, le type actuel reste.</summary>
        private JObject Current() => new()
        {
            ["name"] = EditPayload.Text(Name),
            ["typeFournisseur"] = SelectedType?.id ?? _initialTypeId,
            ["adresse"] = EditPayload.Text(Adresse),
            ["ville"] = EditPayload.Text(Ville),
            ["pays"] = EditPayload.Text(Pays),
            ["tel"] = EditPayload.Text(Tel),
            ["currency"] = SelectedCurrency == SiteCurrencyChoice ? null : SelectedCurrency,
        };

        /// <summary>Ce qui partira au serveur, rien si rien n'a changé.</summary>
        public JObject Changes() => EditPayload.Changes(_initial, Current());

        [RelayCommand]
        private async Task Save()
        {
            if (IsBusy)
                return;

            ErrorText = null;
            if (EditPayload.Text(Name) == null)
            {
                ErrorText = "Le nom du fournisseur est obligatoire.";
                return;
            }

            var changes = Changes();
            if (!changes.HasValues)
            {
                await _dialogService.ShowInfoAsync("Aucun changement à enregistrer.");
                return;
            }

            IsBusy = true;
            try
            {
                await _editService.UpdateFournisseurAsync(SupplierId, changes);
                await _dialogService.ShowInfoAsync("Fournisseur modifié.");
                await GoBackAsync();
            }
            catch (ApiException ex) when (ex.ApiError.Code == "FOURNISSEUR_CURRENCY_LOCKED")
            {
                ErrorText = ex.Message;
                SelectedCurrency = _initialCurrencyChoice;
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
