namespace Easymakemoney.UseCase.ListFournisseurUseCase
{
    public class GetListTypeFournisseurUseCase 
    {
        private readonly IListFourniseursServices _listFourniseursServices;

        public GetListTypeFournisseurUseCase(IListFourniseursServices listFourniseursServices)
        {
            _listFourniseursServices = listFourniseursServices;
        }

        public async Task<ObservableCollection<TypeFournisseur>> ExecuteAsync()
        {
            return await _listFourniseursServices.GetTypeFournisseurs();
        }
        
    }
}