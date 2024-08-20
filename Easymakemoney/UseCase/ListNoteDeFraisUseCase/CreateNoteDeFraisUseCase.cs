namespace Easymakemoney.UseCase.ListNoteDeFraisUseCase
{
    public class CreateNoteDeFraisUseCase
    {
         private readonly IListNoteDeFraisServices _listNoteDeFraisServices;

        public CreateNoteDeFraisUseCase(IListNoteDeFraisServices listNoteDeFraisServices)
        {
                  _listNoteDeFraisServices = listNoteDeFraisServices;
        }

        public async Task<bool> ExecuteAsync(NoteDeFrais newNoteDeFrais, int id)
        {
            return await _listNoteDeFraisServices.PostNoteDeFrais(newNoteDeFrais, id);
        }
    }
}