namespace Easymakemoney.UseCase.CollectionCloseUseCase
{
    public class GetCollectionCloseUseCase 
    {
        private readonly ICollectionCloseService _collectionCloseService;

        public GetCollectionCloseUseCase(ICollectionCloseService collectionCloseService)
        {
            _collectionCloseService = collectionCloseService;
        }

        public async Task<bool> CloseCollectionAsync(int collectionId)
        {
            return await _collectionCloseService.CloseCollectionAsync(collectionId);
        }
    }
}