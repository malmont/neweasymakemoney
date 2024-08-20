namespace Easymakemoney.UseCase.ListNoteDeFraisUseCase
{
    public class DeleteNoteDeFraisUseCase
    {
          private readonly IListNoteDeFraisServices _listNoteDeFraisServices;

        public DeleteNoteDeFraisUseCase(IListNoteDeFraisServices listNoteDeFraisServices)
        {
                _listNoteDeFraisServices = listNoteDeFraisServices;
        }

        public async Task<bool> ExecuteAsync(int id)
        {
            return await _listNoteDeFraisServices.DeleteNoteDeFrais(id);
        }
      
    }
}