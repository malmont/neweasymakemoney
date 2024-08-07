
namespace Easymakemoney.Services
{
    public interface IListCollectionService
    {
        Task<ObservableCollection<ListCollection>> GetCollectionList();
         Task<bool> PostCollection(ListCollection newCollection);
         Task<bool> DeleteCollection(int id);
    }
}
