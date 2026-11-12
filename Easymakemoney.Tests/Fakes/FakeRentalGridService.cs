using Easymakemoney.Models;
using Easymakemoney.Services;

namespace Easymakemoney.Tests.Fakes;

public class FakeRentalGridService : IRentalGridService
{
    public int GetGridsCalls { get; private set; }
    public List<RentalGridRequest> PreviewCalls { get; } = new();
    public List<(RentalGridRequest Request, string PreviewHash, bool AllowShared)> ApplyCalls { get; } = new();
    public List<int> UndoCalls { get; } = new();
    public List<int> GetModificationsCalls { get; } = new();

    public List<RentalGrid> GridsResult { get; set; } = new();
    public RentalGridPreview PreviewResult { get; set; } = new();
    public List<RentalGridModification> ModificationsResult { get; set; } = new();

    public Exception? GetGridsException { get; set; }
    public Exception? PreviewException { get; set; }
    public Exception? ApplyException { get; set; }
    public Exception? UndoException { get; set; }
    public Exception? GetModificationsException { get; set; }

    public Task<List<RentalGrid>> GetGridsAsync()
    {
        GetGridsCalls++;
        if (GetGridsException != null)
            throw GetGridsException;
        return Task.FromResult(GridsResult);
    }

    public Task<RentalGridPreview> PreviewAsync(RentalGridRequest request)
    {
        PreviewCalls.Add(request);
        if (PreviewException != null)
            throw PreviewException;
        return Task.FromResult(PreviewResult);
    }

    public Task ApplyAsync(RentalGridRequest request, string previewHash, bool allowShared)
    {
        ApplyCalls.Add((request, previewHash, allowShared));
        if (ApplyException != null)
            throw ApplyException;
        return Task.CompletedTask;
    }

    public Task UndoAsync(int changeId)
    {
        UndoCalls.Add(changeId);
        if (UndoException != null)
            throw UndoException;
        return Task.CompletedTask;
    }

    public Task<List<RentalGridModification>> GetModificationsAsync(int limit = 10)
    {
        GetModificationsCalls.Add(limit);
        if (GetModificationsException != null)
            throw GetModificationsException;
        return Task.FromResult(ModificationsResult);
    }
}

