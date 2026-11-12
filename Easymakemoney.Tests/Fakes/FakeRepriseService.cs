using Easymakemoney.Models;
using Easymakemoney.Services;

namespace Easymakemoney.Tests.Fakes;

/// <summary>Faux service de reprise d'historique enregistrant simulation, application et annulation.</summary>
public class FakeRepriseService : IRepriseService
{
    public RepriseState StateToReturn { get; set; } = new();
    public RepriseSimulation SimulationToReturn { get; set; } = new();

    public Exception? GetStateException { get; set; }
    public Exception? SimulateException { get; set; }
    public Exception? ApplyException { get; set; }
    public Exception? CancelException { get; set; }

    public int GetStateCalls { get; private set; }
    public List<(int Months, double? Coefficient, int Page, int Limit)> SimulateCalls { get; } = new();
    public List<(int Months, double? Coefficient, List<RepriseProduct> EditedProducts)> ApplyCalls { get; } = new();
    public int CancelCalls { get; private set; }

    public Func<Task>? OnApply { get; set; }
    public Func<Task>? OnCancel { get; set; }

    public Task<RepriseState> GetStateAsync()
    {
        GetStateCalls++;
        if (GetStateException != null)
            throw GetStateException;
        return Task.FromResult(StateToReturn);
    }

    public Task<RepriseSimulation> SimulateAsync(int months, double? coefficient, int page, int limit)
    {
        SimulateCalls.Add((months, coefficient, page, limit));
        if (SimulateException != null)
            throw SimulateException;
        return Task.FromResult(SimulationToReturn);
    }

    public async Task ApplyAsync(int months, double? coefficient, IEnumerable<RepriseProduct> editedProducts)
    {
        ApplyCalls.Add((months, coefficient, editedProducts.ToList()));
        if (ApplyException != null)
            throw ApplyException;
        if (OnApply != null)
            await OnApply();
    }

    public async Task CancelAsync()
    {
        CancelCalls++;
        if (CancelException != null)
            throw CancelException;
        if (OnCancel != null)
            await OnCancel();
    }
}

