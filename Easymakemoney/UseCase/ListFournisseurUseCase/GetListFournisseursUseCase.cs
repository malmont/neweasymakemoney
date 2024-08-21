namespace Easymakemoney.UseCase.ListFournisseurUseCase
{
    public class GetListFournisseursUseCase 
    {
        private readonly IListFourniseursServices _listFourniseursServices;

        public GetListFournisseursUseCase(IListFourniseursServices listFourniseursServices)
        {
            _listFourniseursServices = listFourniseursServices;
        }

        public async Task<ObservableCollection<ListFournisseur>> ExecuteAsync()
        {
            return await _listFourniseursServices.GetFourniseursList();
        }
        
    }
}