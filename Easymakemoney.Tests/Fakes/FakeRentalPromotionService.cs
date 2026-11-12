using Easymakemoney.Models;
using Easymakemoney.Services;

namespace Easymakemoney.Tests.Fakes;

public class FakeRentalPromotionService : IRentalPromotionService
{
    public List<RentalPromotionRequest> PreviewCalls { get; } = new();
    public List<(RentalPromotionRequest Request, string PreviewHash)> ApplyCalls { get; } = new();
    public List<int> EndCalls { get; } = new();
    public int GetPromotionsCalls { get; private set; }

    public RentalPromotionPreview PreviewResult { get; set; } = new();
    public RentalPromotionApplyResult ApplyResult { get; set; } = new();
    public List<RentalPromotionItem> PromotionsResult { get; set; } = new();

    public Exception? PreviewException { get; set; }
    public Exception? ApplyException { get; set; }
    public Exception? EndException { get; set; }
    public Exception? GetPromotionsException { get; set; }

    public Task<RentalPromotionPreview> PreviewAsync(RentalPromotionRequest request)
    {
        PreviewCalls.Add(request);
        if (PreviewException != null)
            throw PreviewException;
        return Task.FromResult(PreviewResult);
    }

    public Task<RentalPromotionApplyResult> ApplyAsync(RentalPromotionRequest request, string previewHash)
    {
        ApplyCalls.Add((request, previewHash));
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

    public Task<List<RentalPromotionItem>> GetPromotionsAsync()
    {
        GetPromotionsCalls++;
        if (GetPromotionsException != null)
            throw GetPromotionsException;
        return Task.FromResult(PromotionsResult);
    }
}

