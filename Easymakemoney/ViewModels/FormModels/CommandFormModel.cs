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
    }
}
