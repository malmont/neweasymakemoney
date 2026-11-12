using System.Collections.ObjectModel;
using Easymakemoney.Models;

namespace Easymakemoney.Tests.Fakes;

public class FakeListNoteDeFraisServices : IListNoteDeFraisServices
{
    public ObservableCollection<TypeNoteDeFrais> TypesResult { get; set; } = new();
    public Exception? GetTypeException { get; set; }

    public Task<ObservableCollection<TypeNoteDeFrais>> GetTypeNoteDeFrais()
    {
        if (GetTypeException != null)
            throw GetTypeException;
        return Task.FromResult(TypesResult);
    }

    public Task<ObservableCollection<NoteDeFrais>> GetNoteDeFraisList(int id) => throw new NotImplementedException();
    public Task<bool> PostNoteDeFrais(NoteDeFrais newNoteDeFrais, int id) => throw new NotImplementedException();
    public Task<bool> DeleteNoteDeFrais(int id) => throw new NotImplementedException();
}
