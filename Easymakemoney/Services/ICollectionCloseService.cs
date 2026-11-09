namespace Easymakemoney.Services
{
    public interface ICollectionCloseService
    {
        Task<bool> CloseCollectionAsync(int collectionId);
        Task<bool> ReopenCollectionAsync(int collectionId);
    }
}