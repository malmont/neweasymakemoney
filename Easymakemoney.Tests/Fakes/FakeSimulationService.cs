using Easymakemoney.Models;
using Easymakemoney.Services;

namespace Easymakemoney.Tests.Fakes;

public class FakeSimulationService : ISimulationService
{
    public List<(int PeriodId, SimulationLevers Levers)> SimulateCalls { get; } = new();
    public List<(int PeriodId, SimulationLevers Levers)> CommentCalls { get; } = new();

    public PeriodSimulation SimulationResult { get; set; } = new();
    public SimulationComment CommentResult { get; set; } = new();

    public Exception? SimulateException { get; set; }
    public Exception? CommentException { get; set; }

    public Task<PeriodSimulation> SimulateAsync(int periodId, SimulationLevers levers)
    {
        SimulateCalls.Add((periodId, levers));
        if (SimulateException != null)
            throw SimulateException;
        return Task.FromResult(SimulationResult);
    }

    public Task<SimulationComment> CommentAsync(int periodId, SimulationLevers levers)
    {
        CommentCalls.Add((periodId, levers));
        if (CommentException != null)
            throw CommentException;
        return Task.FromResult(CommentResult);
    }
}
