namespace Easymakemoney.Services
{
    public interface ICollectionCloseService
    {
        Task<string> GetCollectionClose(int CollectionId);
    }
}