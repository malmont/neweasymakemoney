using System.Collections.ObjectModel;
using Easymakemoney.Models;

namespace Easymakemoney.Tests.Fakes;

public class FakeListNoteDeFraisServices : IListNoteDeFraisServices
{
    public ObservableCollection<TypeNoteDeFrais> TypesResult { get; set; } = new();
    public ObservableCollection<NoteDeFrais> NotesResult { get; set; } = new();
    public List<int> GetCalls { get; } = new();
    public List<(NoteDeFrais Note, int CollectionId)> PostCalls { get; } = new();
    public List<int> DeleteCalls { get; } = new();

    public Exception? GetTypeException { get; set; }
    public Exception? GetException { get; set; }
    public Exception? PostException { get; set; }
    public Exception? DeleteException { get; set; }
    public bool PostResult { get; set; } = true;
    public bool DeleteResult { get; set; } = true;

    public Task<ObservableCollection<TypeNoteDeFrais>> GetTypeNoteDeFrais()
    {
        if (GetTypeException != null)
            throw GetTypeException;
        return Task.FromResult(TypesResult);
    }

    public Task<ObservableCollection<NoteDeFrais>> GetNoteDeFraisList(int id)
    {
        GetCalls.Add(id);
        if (GetException != null)
            throw GetException;
        return Task.FromResult(NotesResult);
    }

    public Task<bool> PostNoteDeFrais(NoteDeFrais newNoteDeFrais, int id)
    {
        PostCalls.Add((newNoteDeFrais, id));
        if (PostException != null)
            throw PostException;
        return Task.FromResult(PostResult);
    }

    public Task<bool> DeleteNoteDeFrais(int id)
    {
        DeleteCalls.Add(id);
        if (DeleteException != null)
            throw DeleteException;
        return Task.FromResult(DeleteResult);
    }
}
