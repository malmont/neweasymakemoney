using Easymakemoney.Models;
using Easymakemoney.Services;

namespace Easymakemoney.Tests.Fakes;

public class FakeToReceiveService : IToReceiveService
{
    public List<ToReceiveLine> Lines { get; set; } = new();
    public List<int> GetLinesCalls { get; } = new();
    public Exception? GetLinesException { get; set; }

    public Task<List<ToReceiveLine>> GetLinesAsync(int limit = 200)
    {
        GetLinesCalls.Add(limit);
        if (GetLinesException != null)
            throw GetLinesException;
        return Task.FromResult(Lines);
    }
}

