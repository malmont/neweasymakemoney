namespace Easymakemoney.UseCase.ListFournisseurUseCase
{
    public class DeleteFournisseurUseCase 
    {
        private readonly IListFourniseursServices _listFourniseursServices;

        public DeleteFournisseurUseCase(IListFourniseursServices listFourniseursServices)
        {
            _listFourniseursServices = listFourniseursServices;
        }

        public async Task<bool> ExecuteAsync(int id)
        {
            return await _listFourniseursServices.DeleteFourniseurs(id);
        }
     
        
    }
}