// Code antérieur aux annotations de nullité : avertissements coupés ici, sans effet sur l'exécution.
#nullable disable warnings


namespace Easymakemoney.Services
{
    public class DashboardCommandServices: IDashboardCommandServices
    {
         private readonly IHttpService _httpService;

        public DashboardCommandServices(IHttpService httpService)
        {
            _httpService = httpService;
        }
     

        public async Task<DashboardCommand> GetDashboardCommand(int CommandId)
        {
            var url = $"dashboard/commande/{CommandId}";
            var dashboardCommand = await _httpService.GetAsync<DashboardCommand>(url);
            return dashboardCommand ?? null;
        }
    }
} 