using System;


namespace Easymakemoney.ViewModels.Lists
{
    public partial class ListNewCollectionViewModel: BaseViewModel
    {
        private readonly IListCollectionService _getListCollection;
        private ObservableCollection<ListCollection> _listCollections;

        public ObservableCollection<ListCollection> ListCollections
        {
            get => _listCollections; 
            set { _listCollections = value; OnPropertyChanged(nameof(ListCollection)); }

        }
        //[ObservableProperty]
        //private ObservableCollection<ListCollection> _listCollections;

        public ListNewCollectionViewModel(IListCollectionService GetListCollection)
		{

            _getListCollection = GetListCollection;
            
            
        }

        public async Task GetListCollectionCommand()
        {
            ListCollections = await _getListCollection.GetCollectionList();
        }

        [ICommand]
        public async Task GetRandomListCollectionAsync()
        {
            if (IsBusy)
                return;

            try
            {
                IsBusy = true;

                var listCollections = await _getListCollection.GetCollectionList();

                ListCollections.Clear();

                foreach (var listCollection in listCollections)
                    ListCollections.Add(listCollection);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                await Shell.Current.DisplayAlert("Error", "Unable to get products", "OK");
            }
            finally
            {
                //IsBusy = false;
                //IsRefreshing = false;
            }
        }

        //public override void loadViewModel()
        //{
        //    GetListCollection();
        //}







    }
}

