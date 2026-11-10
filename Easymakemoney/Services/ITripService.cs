namespace Easymakemoney.Services
{
    public interface ITripService
    {
        Task<List<Trip>> GetTripsAsync(int page = 1, int limit = 30);
        Task<Trip> GetTripAsync(int tripId);
        Task<TripFollowUp> GetFollowUpAsync(int tripId);
        Task<Trip> CreateAsync(TripRequest request);
        Task<Trip> UpdateAsync(int tripId, TripRequest request);
        Task DeleteAsync(int tripId);
    }

    public class TripService : ITripService
    {
        private readonly IHttpService _httpService;

        public TripService(IHttpService httpService)
        {
            _httpService = httpService;
        }

        public async Task<List<Trip>> GetTripsAsync(int page = 1, int limit = 30) =>
            await _httpService.GetAsync<List<Trip>>($"gestion/voyages?page={page}&limit={limit}") ?? new();

        public Task<Trip> GetTripAsync(int tripId) => _httpService.GetAsync<Trip>($"gestion/voyages/{tripId}");

        public Task<TripFollowUp> GetFollowUpAsync(int tripId) => _httpService.GetAsync<TripFollowUp>($"gestion/voyages/{tripId}/suivi");

        public Task<Trip> CreateAsync(TripRequest request) => _httpService.PostAsyncWithAuth<Trip>("gestion/voyages", request);

        /// <summary>PATCH avec tous les champs : le formulaire renvoie le voyage complet.</summary>
        public Task<Trip> UpdateAsync(int tripId, TripRequest request) => _httpService.PatchAsyncWithAuth<Trip>($"gestion/voyages/{tripId}", request);

        public async Task DeleteAsync(int tripId) => await _httpService.DeleAsyncWithAuth<object>($"gestion/voyages/{tripId}");
    }
}
