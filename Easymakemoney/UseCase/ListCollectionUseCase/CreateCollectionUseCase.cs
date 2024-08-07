
namespace Easymakemoney.UseCases
{
    public class CreateCollectionUseCase
    {
        private readonly IListCollectionService _listCollectionService;

        public CreateCollectionUseCase(IListCollectionService listCollectionService)
        {
            _listCollectionService = listCollectionService;
        }

           public async Task<bool> ExecuteAsync(ListCollection newCollection)
        {
            var result = await _listCollectionService.PostCollection(newCollection);
            if (result)
            {
                Debug.WriteLine("Collection created successfully.");
                return true;
            }
            else
            {
                Debug.WriteLine("Failed to create collection.");
                return false;
            }
        }
    }
}
