namespace Easymakemoney.ViewModels.FormModels
{
    public class FournisseurFormModel:ObservableObject
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
    }
}