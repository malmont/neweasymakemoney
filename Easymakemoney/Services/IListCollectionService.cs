
namespace Easymakemoney.Services
{
    public interface IListCollectionService
    {
        Task<ObservableCollection<ListCollection>> GetCollectionList();
    }
}
