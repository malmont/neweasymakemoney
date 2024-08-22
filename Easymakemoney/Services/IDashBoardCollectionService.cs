

namespace Easymakemoney.Services
{
    public interface IDashBoardCollectionService
    {
        Task<DashBoardCollection> GetDashBoardCollection(int CollectionId);
    }
}