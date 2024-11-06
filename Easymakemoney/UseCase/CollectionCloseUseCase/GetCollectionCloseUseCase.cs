namespace Easymakemoney.UseCase.CollectionCloseUseCase
{
    public class GetCollectionCloseUseCase 
    {
        private readonly ICollectionCloseService _collectionCloseService;

        public GetCollectionCloseUseCase(ICollectionCloseService collectionCloseService)
        {
            _collectionCloseService = collectionCloseService;
        }

        public async Task<string> GetCollectionClose(int CollectionId)
        {
            return await _collectionCloseService.GetCollectionClose(CollectionId);
        }
    }
}