namespace Easymakemoney.Services
{
    public interface IListProductService
    {
        Task<ObservableCollection<ListProduct>> GetProductsList(int id);
        Task<bool> PostProducts(ListProduct newProducts, int id);
        Task<bool> DeleteProducts(int id);
    }
}