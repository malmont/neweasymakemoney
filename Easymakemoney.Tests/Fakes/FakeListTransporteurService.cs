using System.Collections.ObjectModel;
using Easymakemoney.Models;
using Easymakemoney.Services;

namespace Easymakemoney.Tests.Fakes;

public class FakeListTransporteurService : IListTransporteurService
{
    public ObservableCollection<Transporteur> TransporteursResult { get; set; } = new();
    public Exception? GetListException { get; set; }

    public Task<ObservableCollection<Transporteur>> GetListTransporteur()
    {
        if (GetListException != null)
            throw GetListException;
        return Task.FromResult(TransporteursResult);
    }
}
