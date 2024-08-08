

namespace Easymakemoney.ViewModels.Lists
{

    public partial class ListNewCommandViewModel : BaseViewModel
    {
     
        private readonly GetListCommandUseCase getListCommandUseCase;

        private int _id;

        public ObservableCollection<ListCommand> ListCommands { get; set; } = new ObservableCollection<ListCommand>();

        public ListNewCommandViewModel(GetListCommandUseCase getListCommandUseCase)
        {
            this.getListCommandUseCase = getListCommandUseCase;
        }

        public async Task GetListCommandAsync(int id)
        {
            _id=id;
            if (IsBusy)
                return;

            IsBusy = true;
            ListCommands.Clear();
            try
            {
                var commands = await getListCommandUseCase.ExecuteAsync(id);
                
                foreach (var command in commands)
                {
                    ListCommands.Add(command);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                await Shell.Current.DisplayAlert("Error", "Unable to get commands", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

    }  
    }

