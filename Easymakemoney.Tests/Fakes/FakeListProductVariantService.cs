using System.Collections.ObjectModel;
using Easymakemoney.Models;
using Easymakemoney.Services;

namespace Easymakemoney.Tests.Fakes;

/// <summary>Faux service des variantes d'un article (lecture, création, suppression).</summary>
public class FakeListProductVariantService : IListProductVarianstService
{
    public List<ProductVariant> Variants { get; } = new();
    public List<int> GetCalls { get; } = new();
    public List<(ProductVariant Variant, int ProductId)> PostCalls { get; } = new();
    public List<int> DeleteCalls { get; } = new();

    public Exception? GetException { get; set; }
    public Exception? PostException { get; set; }
    public Exception? DeleteException { get; set; }
    public bool PostResult { get; set; } = true;
    public bool DeleteResult { get; set; } = true;

    public Task<ObservableCollection<ProductVariant>> GetProductVariantList(int id)
    {
        GetCalls.Add(id);
        if (GetException != null)
            throw GetException;
        return Task.FromResult(new ObservableCollection<ProductVariant>(Variants));
    }

    public Task<bool> PostProductVariant(ProductVariant newProductVariant, int id)
    {
        PostCalls.Add((newProductVariant, id));
        if (PostException != null)
            throw PostException;
        if (PostResult)
        {
            var nextId = Variants.Count > 0 ? Variants.Max(v => v.id) + 1 : 1;
            Variants.Add(new ProductVariant
            {
                id = newProductVariant.id > 0 ? newProductVariant.id : nextId,
                color = newProductVariant.color,
                size = newProductVariant.size,
                stockQuantity = newProductVariant.stockQuantity
            });
        }
        return Task.FromResult(PostResult);
    }

    public Task<bool> DeleteProductVariant(int id)
    {
        DeleteCalls.Add(id);
        if (DeleteException != null)
            throw DeleteException;
        if (DeleteResult)
        {
            Variants.RemoveAll(v => v.id == id);
        }
        return Task.FromResult(DeleteResult);
    }
}

