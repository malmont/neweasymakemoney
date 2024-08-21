namespace Easymakemoney.UseCase.ListFraisDePortUseCase
{
    public class DeleteFraisDePortUseCase
    {
        private readonly IListFraisDePortService _listFraisDePortService;

        public DeleteFraisDePortUseCase(IListFraisDePortService listFraisDePortService)
        {
            _listFraisDePortService = listFraisDePortService;
        }

        public async Task<bool> ExecuteAsync(int id)
        {
            return await _listFraisDePortService.DeleteFraisDePort(id);
        }
    }
    
}