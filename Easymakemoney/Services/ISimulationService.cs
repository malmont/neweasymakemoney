namespace Easymakemoney.Services
{
    public interface ISimulationService
    {
        Task<PeriodSimulation> SimulateAsync(int periodId, SimulationLevers levers);

        /// <summary>Avis du guide : répartitions du prévu calculées par le backend, l'IA en recommande une. Rien n'est écrit.</summary>
        Task<SimulationComment> CommentAsync(int periodId, SimulationLevers levers);
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

        public async Task<SimulationComment> CommentAsync(int periodId, SimulationLevers levers) =>
            (await _httpService.PostAsyncWithAuth<SimulationComment>($"periodes/{periodId}/simulation/commentaire?locale=fr", levers)).Prepare();
    }
}
