

namespace Easymakemoney.ViewModels.FormModels
{
   public class FraisDePortFormModel:ObservableObject
   {
       private string _name;
       public string Name
       {
           get => _name;
           set { _name = value; OnPropertyChanged(nameof(Name)); }
       }

       private string _facture;
         public string Facture
         {
              get => _facture;
              set { _facture = value; OnPropertyChanged(nameof(Facture)); }
         }

       private string _tracknumber;
         public string Tracknumber
         {
              get => _tracknumber;
              set { _tracknumber = value; OnPropertyChanged(nameof(Tracknumber)); }
         }

       private string _image;
            public string Image
            {
                get => _image;
                set { _image = value; OnPropertyChanged(nameof(Image)); }
            }

      public ObservableCollection<Transporteur> Transporteurs { get; set; } = new ObservableCollection<Transporteur>();
          
            private Transporteur _selectedTransporteur;
            public Transporteur SelectedTransporteur
            {
                get => _selectedTransporteur;
                set { _selectedTransporteur = value; OnPropertyChanged(nameof(SelectedTransporteur)); }
            }
    
            private readonly GetListTransporteurUseCase _getListTransporteurUseCase;
    
            public FraisDePortFormModel(GetListTransporteurUseCase getListTransporteurUseCase)
            {
                _getListTransporteurUseCase = getListTransporteurUseCase;
                LoadTransporteur();
            }
    
            private async void LoadTransporteur()
            {
                // Call API to get categories and populate the Categories collection
                var transporteurs = await _getListTransporteurUseCase.ExecuteAsync();
                Transporteurs.Clear();
                foreach (var transporteur in transporteurs)
                {
                    Transporteurs.Add(transporteur);
                }
            }
   }
}