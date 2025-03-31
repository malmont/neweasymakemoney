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

        private double? _montant;
        public double? Montant
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

        public  ObservableCollection<TypeNoteDeFrais> TypeNoteDeFraisSelected { get; set; } = new ObservableCollection<TypeNoteDeFrais>();
        private TypeNoteDeFrais _selectedTypeNoteDeFrais;
        public TypeNoteDeFrais SelectedTypeNoteDeFrais
        {
            get => _selectedTypeNoteDeFrais;
            set { _selectedTypeNoteDeFrais = value; OnPropertyChanged(nameof(SelectedTypeNoteDeFrais)); }
        }

        private readonly GestListTypeNoteDeFraisUseCase _getListTypeNoteDeFraisUseCase;

        public NoteDeFraisForModel(GestListTypeNoteDeFraisUseCase getListTypeNoteDeFraisUseCase)
        {
            Date = DateTime.Today; 
            _getListTypeNoteDeFraisUseCase = getListTypeNoteDeFraisUseCase;
            LoadTypeNoteDeFraisAsync();
        }
        public async Task LoadTypeNoteDeFraisAsync()
        {
            var typeNoteDeFrais = await _getListTypeNoteDeFraisUseCase.ExecuteAsync();
            TypeNoteDeFraisSelected.Clear();
            foreach (var type in typeNoteDeFrais)
            {
                TypeNoteDeFraisSelected.Add(type);
            }
        }

    }
  
        
}