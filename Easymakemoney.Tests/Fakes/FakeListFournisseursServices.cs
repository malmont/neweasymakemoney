using System.Collections.ObjectModel;
using Easymakemoney.Models;
using Easymakemoney.Services;

namespace Easymakemoney.Tests.Fakes;

public class FakeListFournisseursServices : IListFourniseursServices
{
    public ObservableCollection<ListFournisseur> Fournisseurs { get; set; } = new();
    public ObservableCollection<TypeFournisseur> Types { get; set; } = new();

    public Exception? GetFournisseursException { get; set; }
    public Exception? GetTypesException { get; set; }
    public Exception? DeleteException { get; set; }
    public bool DeleteResult { get; set; } = true;

    public List<ListFournisseur> PostCalls { get; } = new();
    public List<int> DeleteCalls { get; } = new();

    public Task<ObservableCollection<ListFournisseur>> GetFourniseursList()
    {
        if (GetFournisseursException != null)
            throw GetFournisseursException;
        return Task.FromResult(Fournisseurs);
    }

    public Task<ObservableCollection<TypeFournisseur>> GetTypeFournisseurs()
    {
        if (GetTypesException != null)
            throw GetTypesException;
        return Task.FromResult(Types);
    }

    public Task<bool> PostFourniseurs(ListFournisseur newFourniseurs)
    {
        PostCalls.Add(newFourniseurs);
        return Task.FromResult(true);
    }

    public Task<bool> DeleteFourniseurs(int id)
    {
        DeleteCalls.Add(id);
        if (DeleteException != null)
            throw DeleteException;
        return Task.FromResult(DeleteResult);
    }
}
