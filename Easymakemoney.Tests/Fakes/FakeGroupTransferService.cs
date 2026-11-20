using Easymakemoney.Models;
using Easymakemoney.Services;

namespace Easymakemoney.Tests.Fakes;

public class FakeGroupTransferService : IGroupTransferService
{
    public List<GroupTransfer> Transfers { get; set; } = new();

    /// <summary>Listes rendues dans l'ordre (une par lecture), sinon Transfers.</summary>
    public Queue<List<GroupTransfer>> TransferResults { get; } = new();

    public int GetCalls { get; private set; }
    public List<int> SendCalls { get; } = new();

    public Exception? GetException { get; set; }
    public Exception? SendException { get; set; }

    public Task<List<GroupTransfer>> GetTransfersToSendAsync()
    {
        GetCalls++;
        if (GetException != null)
            throw GetException;
        return Task.FromResult(TransferResults.Count > 0 ? TransferResults.Dequeue() : Transfers);
    }

    public Task SendAsync(int transferId)
    {
        SendCalls.Add(transferId);
        if (SendException != null)
            throw SendException;
        return Task.CompletedTask;
    }
}

public class FakeGroupService : IGroupService
{
    public bool IsInGroup { get; set; }
    public Exception? Exception { get; set; }
    public int Calls { get; private set; }

    public Task<bool> IsInGroupAsync()
    {
        Calls++;
        if (Exception != null)
            throw Exception;
        return Task.FromResult(IsInGroup);
    }
}
