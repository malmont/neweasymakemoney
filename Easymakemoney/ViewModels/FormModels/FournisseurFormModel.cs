namespace Easymakemoney.ViewModels.FormModels
{
    public class FournisseurFormModel : ObservableObject
    {
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

        private string _adresse;
        public string Adresse
        {
            get => _adresse;
            set { _adresse = value; OnPropertyChanged(nameof(Adresse)); }
        }

        private string _ville;
        public string Ville
        {
            get => _ville;
            set { _ville = value; OnPropertyChanged(nameof(Ville)); }
        }

        private string _pays;
        public string Pays
        {
            get => _pays;
            set { _pays = value; OnPropertyChanged(nameof(Pays)); }
        }

        private string _tel;
        public string Tel
        {
            get => _tel;
            set { _tel = value; OnPropertyChanged(nameof(Tel)); }

        }

        public ObservableCollection<TypeFournisseur> TypeFournisseurSelected { get; set; } = new ObservableCollection<TypeFournisseur>();
          
            private TypeFournisseur _selectedTypeFournisseur;
            public TypeFournisseur SelectedTypeFournisseur
            {
                get => _selectedTypeFournisseur;
                set { _selectedTypeFournisseur = value; OnPropertyChanged(nameof(SelectedTypeFournisseur)); }
            }
    
            private readonly GetListTypeFournisseurUseCase _getListTypeFournisseurUseCase;
    
            public FournisseurFormModel(GetListTypeFournisseurUseCase getListTypeFournisseurUseCase)
            {
                _getListTypeFournisseurUseCase = getListTypeFournisseurUseCase;
                LoadTypeFournisseur();
            }
    
            private async void LoadTypeFournisseur()
            {
                var typeFournisseurs = await _getListTypeFournisseurUseCase.ExecuteAsync();
                TypeFournisseurSelected.Clear();
                foreach (var typeFournisseur in typeFournisseurs)
                {
                    TypeFournisseurSelected.Add(typeFournisseur);
                }
            }
    }
}