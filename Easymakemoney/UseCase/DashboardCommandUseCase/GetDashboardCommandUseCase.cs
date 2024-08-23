namespace Easymakemoney.UseCase.DashboardCommandUseCase
{
    public class GetDashboardCommandUseCase
    {
        private readonly IDashboardCommandServices _dashboardCommandServices;

        public GetDashboardCommandUseCase(IDashboardCommandServices dashboardCommandServices)
        {
            _dashboardCommandServices = dashboardCommandServices;
        }

        public async Task<DashboardCommand> GetDashboardCommand(int CommandId)
        {
            return await _dashboardCommandServices.GetDashboardCommand(CommandId);
        }
    }
}