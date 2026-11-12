using Easymakemoney.Models;
using Easymakemoney.Services;

namespace Easymakemoney.Tests.Fakes;

public class FakeListFraisDePortService : IListFraisDePortService
{
    public Dictionary<int, FraisDePort?> FraisByCommandId { get; } = new();
    public List<int> GetCalls { get; } = new();
    public List<(FraisDePort Frais, int CommandId)> PostCalls { get; } = new();
    public List<int> DeleteCalls { get; } = new();

    public Exception? GetException { get; set; }
    public Exception? PostException { get; set; }
    public Exception? DeleteException { get; set; }
    public bool DeleteResult { get; set; } = true;
    public bool PostResult { get; set; } = true;

    public Task<FraisDePort?> GetListFraisDePort(int commandId)
    {
        GetCalls.Add(commandId);
        if (GetException != null)
            throw GetException;
        FraisByCommandId.TryGetValue(commandId, out var frais);
        return Task.FromResult(frais);
    }

    public Task<bool> PostFraisDePort(FraisDePort newFraisDePort, int commandId)
    {
        PostCalls.Add((newFraisDePort, commandId));
        if (PostException != null)
            throw PostException;
        return Task.FromResult(PostResult);
    }

    public Task<bool> DeleteFraisDePort(int commandId)
    {
        DeleteCalls.Add(commandId);
        if (DeleteException != null)
            throw DeleteException;
        return Task.FromResult(DeleteResult);
    }
}

