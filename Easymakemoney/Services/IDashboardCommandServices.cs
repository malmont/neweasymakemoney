namespace Easymakemoney.Services
{
    public interface IDashboardCommandServices
    {
        Task<DashboardCommand> GetDashboardCommand(int CommandId);
    }
}