using Easymakemoney.Models;
using Easymakemoney.Services;

namespace Easymakemoney.Tests.Fakes;

public class FakePreparationService : IPreparationService
{
    public List<string> GetEstimatesCalls { get; } = new();
    public List<PreparationRequest> SimulateCalls { get; } = new();
    public List<PreparationRequest> AdviseCalls { get; } = new();
    public List<(PreparationRequest Request, string PreviewHash)> ValidateCalls { get; } = new();

    public PreparationEstimates EstimatesResult { get; set; } = new();
    public PreparationSimulation SimulationResult { get; set; } = new();
    public PreparationAdvice AdviceResult { get; set; } = new();

    public Exception? GetEstimatesException { get; set; }
    public Exception? SimulateException { get; set; }
    public Exception? AdviseException { get; set; }
    public Exception? ValidateException { get; set; }

    public Task<PreparationEstimates> GetEstimatesAsync(string date)
    {
        GetEstimatesCalls.Add(date);
        if (GetEstimatesException != null)
            throw GetEstimatesException;
        return Task.FromResult(EstimatesResult);
    }

    public Task<PreparationSimulation> SimulateAsync(PreparationRequest request)
    {
        SimulateCalls.Add(request);
        if (SimulateException != null)
            throw SimulateException;
        return Task.FromResult(SimulationResult);
    }

    public Task<PreparationAdvice> AdviseAsync(PreparationRequest request)
    {
        AdviseCalls.Add(request);
        if (AdviseException != null)
            throw AdviseException;
        return Task.FromResult(AdviceResult);
    }

    public Task ValidateAsync(PreparationRequest request, string previewHash)
    {
        ValidateCalls.Add((request, previewHash));
        if (ValidateException != null)
            throw ValidateException;
        return Task.CompletedTask;
    }
}
