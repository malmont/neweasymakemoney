namespace Easymakemoney.UseCase.ListTransporteurUseCase
{
    public class GetListTransporteurUseCase 
    {
        private readonly IListTransporteurService _listTransporteurService;

        public GetListTransporteurUseCase(IListTransporteurService listTransporteurService)
        {
            _listTransporteurService = listTransporteurService;
        }

        public async Task<ObservableCollection<Transporteur>> ExecuteAsync()
        {
            return await _listTransporteurService.GetListTransporteur();
        }
    }
}