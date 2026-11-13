using System.Collections.ObjectModel;
using Easymakemoney.Models;
using Easymakemoney.Services;

namespace Easymakemoney.Tests.Fakes;

/// <summary>Faux service de lecture des catégories d'article.</summary>
public class FakeListCategoryServices : IListCategoryServices
{
    public ObservableCollection<Category> CategoriesResult { get; set; } = new();
    public Exception? GetException { get; set; }
    public int GetCalls { get; private set; }

    public Task<ObservableCollection<Category>> GetCategoryList()
    {
        GetCalls++;
        if (GetException != null)
            throw GetException;
        return Task.FromResult(CategoriesResult);
    }
}

