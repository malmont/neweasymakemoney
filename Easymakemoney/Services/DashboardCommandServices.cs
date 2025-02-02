

namespace Easymakemoney.Services
{
    public class DashboardCommandServices: IDashboardCommandServices
    {
        private const string DashBoardCommandUrl = Configurations.BackendSymfonyUrl;
         private readonly IHttpService _httpService;

        public DashboardCommandServices(IHttpService httpService)
        {
            _httpService = httpService;
        }
     

        public async Task<DashboardCommand> GetDashboardCommand(int CommandId)
        {
            var url = $"{DashBoardCommandUrl}/dashboard/commande/{CommandId}";
            var dashboardCommand = await _httpService.GetAsync<DashboardCommand>(url);
            return dashboardCommand ?? null;
        }
    }
} 