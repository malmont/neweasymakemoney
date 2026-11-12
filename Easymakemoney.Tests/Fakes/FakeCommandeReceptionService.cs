using Easymakemoney.Models;
using Easymakemoney.Services;
using Newtonsoft.Json.Linq;

namespace Easymakemoney.Tests.Fakes;

public class FakeCommandeReceptionService : ICommandeReceptionService
{
    public List<(int CommandeId, JObject? Body)> ReceiveCalls { get; } = new();

    public CommandeReception ReceiveResult { get; set; } = new();
    public Queue<CommandeReception> ReceiveResults { get; } = new();
    public Exception? ReceiveException { get; set; }

    public Task<CommandeReception> ReceiveAsync(int commandeId, JObject? body = null)
    {
        ReceiveCalls.Add((commandeId, body));
        if (ReceiveException != null)
            throw ReceiveException;
        if (ReceiveResults.Count > 0)
            return Task.FromResult(ReceiveResults.Dequeue());
        return Task.FromResult(ReceiveResult);
    }
}
