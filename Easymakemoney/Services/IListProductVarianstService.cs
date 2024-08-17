namespace Easymakemoney.Services
{
    public interface IListProductVarianstService
    {
        Task<ObservableCollection<ProductVariant>> GetProductVariantList(int id);
        Task<bool> PostProductVariant(ProductVariant newProductVariant,int id);
        Task<bool> DeleteProductVariant(int id);

    }
}