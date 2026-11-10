namespace Easymakemoney.Services
{
    public interface ISimulationService
    {
        Task<PeriodSimulation> SimulateAsync(int periodId, SimulationLevers levers);
    }

    public class SimulationService : ISimulationService
    {
        private readonly IHttpService _httpService;

        public SimulationService(IHttpService httpService)
        {
            _httpService = httpService;
        }

        /// <summary>POST sans écriture côté backend ; textes en français comme le reste de l'application.</summary>
        public Task<PeriodSimulation> SimulateAsync(int periodId, SimulationLevers levers) =>
            _httpService.PostAsyncWithAuth<PeriodSimulation>($"periodes/{periodId}/simulation?locale=fr", levers);
    }
}
