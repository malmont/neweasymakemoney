using Easymakemoney.Models.DashboardCommand;
using Easymakemoney.Services;

namespace Easymakemoney.Tests.Fakes;

/// <summary>Faux service du tableau de bord d'une commande.</summary>
public class FakeDashboardCommandServices : IDashboardCommandServices
{
    public DashboardCommand? Result { get; set; }
    public TaskCompletionSource<DashboardCommand>? PendingTask { get; set; }
    public Exception? ExceptionToThrow { get; set; }
    public List<int> RequestedIds { get; } = new();

    public Task<DashboardCommand> GetDashboardCommand(int CommandId)
    {
        RequestedIds.Add(CommandId);
        if (ExceptionToThrow != null)
            throw ExceptionToThrow;
        if (PendingTask != null)
            return PendingTask.Task;
        return Task.FromResult(Result!);
    }
}

