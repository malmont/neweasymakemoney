
namespace Easymakemoney.UseCase.ListCollectionUsecase
{
    public class DeleteCollectionUseCase
    {
        private readonly IListCollectionService _listCollectionService;

        public DeleteCollectionUseCase(IListCollectionService listCollectionService)
        {
            _listCollectionService = listCollectionService;
        }

           public async Task<bool> ExecuteAsync(int id)
        {
            var result = await _listCollectionService.DeleteCollection(id);
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
