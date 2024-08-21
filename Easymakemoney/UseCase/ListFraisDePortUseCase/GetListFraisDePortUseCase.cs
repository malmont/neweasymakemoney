namespace Easymakemoney.UseCase.ListFraisDePortUseCase
{
    public class GetListFraisDePortUseCase
    {
        private readonly IListFraisDePortService _listFraisDePortService;

        public GetListFraisDePortUseCase(IListFraisDePortService listFraisDePortService)
        {
            _listFraisDePortService = listFraisDePortService;
        }

        public async Task<ObservableCollection<FraisDePort>> ExecuteAsync(int commandId)
        {
            var newFraisDePort = new ObservableCollection<FraisDePort>();
            var listFraisDePort = await _listFraisDePortService.GetListFraisDePort(commandId);
            newFraisDePort.Add(listFraisDePort);
            return newFraisDePort??null;
        }
    }
}