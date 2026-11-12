using Easymakemoney.Models;
using Easymakemoney.Services;

namespace Easymakemoney.Tests.Fakes;

public class FakeVariantPromotionService : IVariantPromotionService
{
    public List<VariantPromotionRequest> PreviewCalls { get; } = new();
    public List<(VariantPromotionRequest Request, string PreviewHash, bool AllowBelowCost, bool AllowBigChange, bool AllowReplace, bool AllowAboveProductPromotion)> ApplyCalls { get; } = new();
    public List<int> EndCalls { get; } = new();
    public List<string> GetPromotionsCalls { get; } = new();

    public VariantPromotionPreview PreviewResult { get; set; } = new();
    public VariantPromotionApplyResult ApplyResult { get; set; } = new();
    public List<VariantPromotionItem> PromotionsResult { get; set; } = new();

    public Exception? PreviewException { get; set; }
    public Exception? ApplyException { get; set; }
    public Exception? EndException { get; set; }
    public Exception? GetPromotionsException { get; set; }

    public Task<VariantPromotionPreview> PreviewAsync(VariantPromotionRequest request)
    {
        PreviewCalls.Add(request);
        if (PreviewException != null)
            throw PreviewException;
        return Task.FromResult(PreviewResult);
    }

    public Task<VariantPromotionApplyResult> ApplyAsync(VariantPromotionRequest request, string previewHash, bool allowBelowCost, bool allowBigChange, bool allowReplace, bool allowAboveProductPromotion)
    {
        ApplyCalls.Add((request, previewHash, allowBelowCost, allowBigChange, allowReplace, allowAboveProductPromotion));
        if (ApplyException != null)
            throw ApplyException;
        return Task.FromResult(ApplyResult);
    }

    public Task EndAsync(int batchId)
    {
        EndCalls.Add(batchId);
        if (EndException != null)
            throw EndException;
        return Task.CompletedTask;
    }

    public Task<List<VariantPromotionItem>> GetPromotionsAsync(string status = "all")
    {
        GetPromotionsCalls.Add(status);
        if (GetPromotionsException != null)
            throw GetPromotionsException;
        return Task.FromResult(PromotionsResult);
    }
}

