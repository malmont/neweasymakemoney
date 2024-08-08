
namespace Easymakemoney.UseCases
{
    public class DeleteCommandUseCase
    {
        private readonly IListCommandService _listCommandService;

        public DeleteCommandUseCase(IListCommandService listCommandService)
        {
            _listCommandService = listCommandService;
        }

           public async Task<bool> ExecuteAsync(int id)
        {
            var result = await _listCommandService.DeleteCommandes(id);
            if (result)
            {
                Debug.WriteLine("Collection deleted successfully.");
                return true;
            }
            else
            {
                Debug.WriteLine("Failed to deleted collection.");
                return false;
            }
        }
    }
}
