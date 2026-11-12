using Easymakemoney.Models;
using Easymakemoney.Services;

namespace Easymakemoney.Tests.Fakes;

public class FakePriceService : IPriceService
{
    public List<PriceChangeRequest> PreviewCalls { get; } = new();
    public List<(PriceChangeRequest Request, string PreviewHash, bool AllowBelowCost, bool AllowBigChange)> ApplyCalls { get; } = new();
    public List<int> GetBatchesCalls { get; } = new();
    public List<(int BatchId, bool OnlyUntouched)> UndoCalls { get; } = new();

    public PricePreview PreviewResult { get; set; } = new();
    public PriceApplyResult ApplyResult { get; set; } = new();
    public List<PriceBatch> BatchesResult { get; set; } = new();
    public PriceUndoResult UndoResult { get; set; } = new();

    public Exception? PreviewException { get; set; }
    public Exception? ApplyException { get; set; }
    public Exception? GetBatchesException { get; set; }
    public Exception? UndoException { get; set; }
    public bool UndoExceptionOnlyInitial { get; set; }

    public Task<PricePreview> PreviewAsync(PriceChangeRequest request)
    {
        PreviewCalls.Add(request);
        if (PreviewException != null)
            throw PreviewException;
        return Task.FromResult(PreviewResult);
    }

    public Task<PriceApplyResult> ApplyAsync(PriceChangeRequest request, string previewHash, bool allowBelowCost, bool allowBigChange)
    {
        ApplyCalls.Add((request, previewHash, allowBelowCost, allowBigChange));
        if (ApplyException != null)
            throw ApplyException;
        return Task.FromResult(ApplyResult);
    }

    public Task<List<PriceBatch>> GetBatchesAsync(int limit = 20)
    {
        GetBatchesCalls.Add(limit);
        if (GetBatchesException != null)
            throw GetBatchesException;
        return Task.FromResult(BatchesResult);
    }

    public Task<PriceUndoResult> UndoAsync(int batchId, bool onlyUntouched = false)
    {
        UndoCalls.Add((batchId, onlyUntouched));
        if (UndoException != null && !(UndoExceptionOnlyInitial && onlyUntouched))
            throw UndoException;
        return Task.FromResult(UndoResult);
    }
}

