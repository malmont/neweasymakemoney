namespace Easymakemoney.UseCases

{
    public class GetListCommandUseCase
    {
        private readonly IListCommandService _listCommandService;

        public GetListCommandUseCase(IListCommandService listCommandService)
        {
            _listCommandService = listCommandService;
        }

        public async Task<ObservableCollection<ListCommand>> ExecuteAsync(int id)
        {
            return await _listCommandService.GetCommandesList(id);
        }
    }
}