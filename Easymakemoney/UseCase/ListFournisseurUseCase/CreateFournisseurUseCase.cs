namespace Easymakemoney.UseCase.ListFournisseurUseCase
{
    public class CreateFournisseurUseCase 
    {
        private readonly IListFourniseursServices _listFourniseursServices;

        public CreateFournisseurUseCase(IListFourniseursServices listFourniseursServices)
        {
            _listFourniseursServices = listFourniseursServices;
        }

        public async Task<bool> ExecuteAsync(ListFournisseur newFourniseurs,int id)
        {
            return await _listFourniseursServices.PostFourniseurs(newFourniseurs,id);
        }
        
    }
}