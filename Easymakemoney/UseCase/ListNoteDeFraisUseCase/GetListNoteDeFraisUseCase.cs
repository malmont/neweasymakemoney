namespace Easymakemoney.UseCase.ListNoteDeFraisUseCase
{
    public class GetListNoteDeFraisUseCase
    {
        private readonly IListNoteDeFraisServices _listNoteDeFraisServices;

        public GetListNoteDeFraisUseCase(IListNoteDeFraisServices listNoteDeFraisServices)
        {
            _listNoteDeFraisServices = listNoteDeFraisServices;
        }

        public async Task<ObservableCollection<NoteDeFrais>> ExecuteAsync(int id)
        {
            return await _listNoteDeFraisServices.GetNoteDeFraisList(id);
        }
     
    }
}