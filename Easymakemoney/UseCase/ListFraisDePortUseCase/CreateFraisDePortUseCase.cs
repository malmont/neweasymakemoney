namespace Easymakemoney.UseCase.ListFraisDePortUseCase
{
    public class CreateFraisDePortUseCase
    {
        private readonly IListFraisDePortService _listFraisDePortService;

        public CreateFraisDePortUseCase(IListFraisDePortService listFraisDePortService)
        {
            _listFraisDePortService = listFraisDePortService;
        }

        public async Task<bool> ExecuteAsync(FraisDePort newFraisDePort, int commandId)
        {
            return await _listFraisDePortService.PostFraisDePort(newFraisDePort, commandId);
        }
    }
}