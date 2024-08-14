namespace EEasymakemoney.UseCase.ListStyleUsesCase
{
    public class GetListStyleUsesCases
    {
        private readonly IListStyleService _listStyleService;

        public GetListStyleUsesCases(IListStyleService listStyleService)
        {
            _listStyleService = listStyleService;
        }

        public async Task<ObservableCollection<Styles>> ExecuteAsync()
        {
            return await _listStyleService.GetStyleList();
        }
    }
}