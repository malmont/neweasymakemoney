namespace Easymakemoney.ViewModels.FormModels
{
    public class NoteDeFraisForModel : ObservableObject
    {
        private string _name;
        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(nameof(Name)); }
        }

        private string _description;
        public string Description
        {
            get => _description;
            set { _description = value; OnPropertyChanged(nameof(Description)); }
        }

        private double _montant;
        public double Montant
        {
            get => _montant;
            set { _montant = value; OnPropertyChanged(nameof(Montant)); }
        }

        private DateTime _date;
        public DateTime Date
        {
            get => _date;
            set { _date = value; OnPropertyChanged(nameof(Date)); }
        }

        private string _imageNdf;
        public string ImageNdf
        {
            get => _imageNdf;
            set { _imageNdf = value; OnPropertyChanged(nameof(ImageNdf)); }


        }
    }
}