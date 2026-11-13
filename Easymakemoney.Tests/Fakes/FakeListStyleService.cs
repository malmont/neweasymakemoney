using System.Collections.ObjectModel;
using Easymakemoney.Models;
using Easymakemoney.Services;

namespace Easymakemoney.Tests.Fakes;

/// <summary>Faux service de lecture des styles d'article.</summary>
public class FakeListStyleService : IListStyleService
{
    public ObservableCollection<Styles> StylesResult { get; set; } = new();
    public Exception? GetException { get; set; }
    public int GetCalls { get; private set; }

    public Task<ObservableCollection<Styles>> GetStyleList()
    {
        GetCalls++;
        if (GetException != null)
            throw GetException;
        return Task.FromResult(StylesResult);
    }
}

