namespace Easymakemoney.Services
{
    public interface IRentalGridService
    {
        Task<List<RentalGrid>> GetGridsAsync();
        Task<RentalGridPreview> PreviewAsync(RentalGridRequest request);
        Task ApplyAsync(RentalGridRequest request, string previewHash, bool allowShared);
        Task UndoAsync(int changeId);
        Task<List<RentalGridModification>> GetModificationsAsync(int limit = 10);
    }

    /// <summary>Changement de grille de location (lot backend LOC4) : aperçu sans écriture, application, annulation 24 h.</summary>
    public class RentalGridService : IRentalGridService
    {
        private readonly IHttpService _httpService;

        public RentalGridService(IHttpService httpService)
        {
            _httpService = httpService;
        }

        public async Task<List<RentalGrid>> GetGridsAsync() =>
            await _httpService.GetAsync<List<RentalGrid>>("gestion/locations/grilles") ?? new List<RentalGrid>();

        public async Task<RentalGridPreview> PreviewAsync(RentalGridRequest request) =>
            await _httpService.PostAsyncWithAuth<RentalGridPreview>("gestion/locations/grilles/apercu", request.ToBody()) ?? new RentalGridPreview();

        public async Task ApplyAsync(RentalGridRequest request, string previewHash, bool allowShared) =>
            await _httpService.PostAsyncWithAuth<object>("gestion/locations/grilles/appliquer", request.ToBody(previewHash, allowShared));

        public async Task UndoAsync(int changeId) =>
            await _httpService.PostAsyncWithAuth<object>("gestion/locations/grilles/annuler", new Dictionary<string, object> { ["changeId"] = changeId });

        public async Task<List<RentalGridModification>> GetModificationsAsync(int limit = 10) =>
            await _httpService.GetAsync<List<RentalGridModification>>($"gestion/locations/grilles/modifications?limit={limit}") ?? new List<RentalGridModification>();
    }
}
