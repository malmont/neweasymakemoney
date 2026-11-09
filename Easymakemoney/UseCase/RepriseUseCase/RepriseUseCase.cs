namespace Easymakemoney.UseCase.RepriseUseCase
{
    public class RepriseUseCase
    {
        private readonly IRepriseService _service;

        public RepriseUseCase(IRepriseService service)
        {
            _service = service;
        }

        public Task<RepriseState> GetStateAsync() => _service.GetStateAsync();

        public Task<RepriseSimulation> SimulateAsync(int months, double? coefficient, int page, int limit) =>
            _service.SimulateAsync(months, coefficient, page, limit);

        public Task ApplyAsync(int months, double? coefficient, IEnumerable<RepriseProduct> editedProducts) =>
            _service.ApplyAsync(months, coefficient, editedProducts);

        public Task CancelAsync() => _service.CancelAsync();
    }
}
