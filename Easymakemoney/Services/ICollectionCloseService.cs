namespace Easymakemoney.Services
{
    public interface ICollectionCloseService
    {
        Task<bool> CloseCollectionAsync(int collectionId);
    }
}