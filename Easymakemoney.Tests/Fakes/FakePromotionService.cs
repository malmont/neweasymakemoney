using Easymakemoney.Models;
using Easymakemoney.Services;

namespace Easymakemoney.Tests.Fakes;

public class FakePromotionService : IPromotionService
{
    public List<PromotionRequest> PreviewCalls { get; } = new();
    public List<(PromotionRequest Request, string PreviewHash, bool AllowBelowCost, bool AllowBigChange, bool AllowReplace)> ApplyCalls { get; } = new();
    public List<string> GetPromotionsCalls { get; } = new();
    public List<(int BatchId, bool OnlyUntouched)> EndCalls { get; } = new();

    public PromotionPreview PreviewResult { get; set; } = new();
    public PriceApplyResult ApplyResult { get; set; } = new();
    public List<PromotionItem> PromotionsResult { get; set; } = new();
    public PromotionEndResult EndResult { get; set; } = new();

    public Exception? PreviewException { get; set; }
    public Exception? ApplyException { get; set; }
    public Exception? GetPromotionsException { get; set; }
    public Exception? EndException { get; set; }
    public bool EndExceptionOnlyInitial { get; set; }

    public Task<PromotionPreview> PreviewAsync(PromotionRequest request)
    {
        PreviewCalls.Add(request);
        if (PreviewException != null)
            throw PreviewException;
        return Task.FromResult(PreviewResult);
    }

    public Task<PriceApplyResult> ApplyAsync(PromotionRequest request, string previewHash, bool allowBelowCost, bool allowBigChange, bool allowReplace)
    {
        ApplyCalls.Add((request, previewHash, allowBelowCost, allowBigChange, allowReplace));
        if (ApplyException != null)
            throw ApplyException;
        return Task.FromResult(ApplyResult);
    }

    public Task<List<PromotionItem>> GetPromotionsAsync(string status = "all")
    {
        GetPromotionsCalls.Add(status);
        if (GetPromotionsException != null)
            throw GetPromotionsException;
        return Task.FromResult(PromotionsResult);
    }

    public Task<PromotionEndResult> EndAsync(int batchId, bool onlyUntouched = false)
    {
        EndCalls.Add((batchId, onlyUntouched));
        if (EndException != null && !(EndExceptionOnlyInitial && onlyUntouched))
            throw EndException;
        return Task.FromResult(EndResult);
    }
}

