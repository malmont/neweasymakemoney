namespace Easymakemoney.Services
{
    public interface IListProductService
    {
        Task<ObservableCollection<ListProduct>> GetProductsList(int id);
        Task<bool> PostProducts(ListProduct newProduct, Stream imageStream, string fileName, int id);
        Task<bool> DeleteProducts(int id);
    }
}