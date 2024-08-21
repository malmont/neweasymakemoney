namespace Easymakemoney.ViewModels.FormModels
{
    public class CommandFormModel : ObservableObject
    {
        private float _budget;
        public float Budget
        {
            get => _budget;
            set { _budget = value; OnPropertyChanged(nameof(Budget)); }
        }

        private DateTime _date;
        public DateTime Date
        {
            get => _date;
            set { _date = value; OnPropertyChanged(nameof(Date)); }
        }

        private string _name;
        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(nameof(Name)); }
        }

        private string _photo;
        public string Photo
        {
            get => _photo;
            set { _photo = value; OnPropertyChanged(nameof(Photo)); }
        }


        public ObservableCollection<ListFournisseur> Fournisseurs { get; set; } = new ObservableCollection<ListFournisseur>();
        private ListFournisseur _selectedFournisseurs;
        public ListFournisseur SelectedFournisseurs
        {
            get => _selectedFournisseurs;
            set { _selectedFournisseurs = value; OnPropertyChanged(nameof(SelectedFournisseurs)); }
        }

        private readonly GetListFournisseursUseCase _getListFournisseursUseCase;

        public CommandFormModel(GetListFournisseursUseCase getListFournisseursUseCase)
        {
            _getListFournisseursUseCase = getListFournisseursUseCase;
            LoadTransporteurs();
        }

        private async void LoadTransporteurs()
        {
            var fournisseurs = await _getListFournisseursUseCase.ExecuteAsync();
            Fournisseurs.Clear();
            foreach (var fournisseur in fournisseurs)
            {
                Fournisseurs.Add(fournisseur);
            }

        }
    }
}
