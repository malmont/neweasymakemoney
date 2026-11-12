using System.Collections.ObjectModel;
using Easymakemoney.Models;
using Easymakemoney.Services;

namespace Easymakemoney.Tests.Fakes;

public class FakeListCommandService : IListCommandService
{
    public ObservableCollection<ListCommand> Commands { get; set; } = new();
    public List<int> GetCalls { get; } = new();
    public List<(ListCommand Command, int CollectionId)> PostCalls { get; } = new();
    public List<int> DeleteCalls { get; } = new();

    public Exception? GetException { get; set; }
    public Exception? PostException { get; set; }
    public Exception? DeleteException { get; set; }
    public bool DeleteResult { get; set; } = true;
    public bool PostResult { get; set; } = true;

    public Task<ObservableCollection<ListCommand>> GetCommandesList(int id)
    {
        GetCalls.Add(id);
        if (GetException != null)
            throw GetException;
        return Task.FromResult(Commands);
    }

    public Task<bool> PostCommandes(ListCommand newCommandes, int id)
    {
        PostCalls.Add((newCommandes, id));
        if (PostException != null)
            throw PostException;
        return Task.FromResult(PostResult);
    }

    public Task<bool> DeleteCommandes(int id)
    {
        DeleteCalls.Add(id);
        if (DeleteException != null)
            throw DeleteException;
        return Task.FromResult(DeleteResult);
    }
}

