
namespace Easymakemoney.Services
{
    public interface IListStyleService
    {
        Task<ObservableCollection<Styles>> GetStyleList();
    }
}