namespace Easymakemoney.UseCase.ListNoteDeFraisUseCase
{
    public class GestListTypeNoteDeFraisUseCase
    {
        private readonly IListNoteDeFraisServices _listNoteDeFraisServices;

        public GestListTypeNoteDeFraisUseCase(IListNoteDeFraisServices listNoteDeFraisServices)
        {
            _listNoteDeFraisServices = listNoteDeFraisServices;
        }

        public async Task<ObservableCollection<TypeNoteDeFrais>> ExecuteAsync()
        {
            return await _listNoteDeFraisServices.GetTypeNoteDeFrais();
        }
    }
}