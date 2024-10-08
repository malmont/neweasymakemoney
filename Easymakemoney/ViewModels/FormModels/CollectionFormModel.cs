namespace Easymakemoney.ViewModels.FormModels
{
    public class CollectionFormModel : ObservableObject
    {
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
    }
}
