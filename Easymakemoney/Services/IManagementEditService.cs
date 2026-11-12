using Newtonsoft.Json.Linq;

namespace Easymakemoney.Services
{
    /// <summary>Lot EDIT (modifications partielles) et lecture B3 bis des frais de port d'une période.</summary>
    public interface IManagementEditService
    {
        /// <summary>PATCH /api/fournisseurs/{id} : seules les clés envoyées changent.</summary>
        Task<ListFournisseur?> UpdateFournisseurAsync(int id, JObject changes);

        /// <summary>PATCH /api/commandes/{id}/frais-de-port : seules les clés envoyées changent.</summary>
        Task<FraisDePort?> UpdateFraisDePortAsync(int commandeId, JObject changes);

        /// <summary>GET /api/collections/{id}/frais-de-port.</summary>
        Task<PeriodShipping?> GetPeriodShippingAsync(int collectionId);
    }

    public class ManagementEditService : IManagementEditService
    {
        private readonly IHttpService _httpService;

        public ManagementEditService(IHttpService httpService)
        {
            _httpService = httpService;
        }

        public Task<ListFournisseur?> UpdateFournisseurAsync(int id, JObject changes) =>
            _httpService.PatchAsyncWithAuth<ListFournisseur?>($"fournisseurs/{id}", changes);

        public Task<FraisDePort?> UpdateFraisDePortAsync(int commandeId, JObject changes) =>
            _httpService.PatchAsyncWithAuth<FraisDePort?>($"commandes/{commandeId}/frais-de-port", changes);

        public Task<PeriodShipping?> GetPeriodShippingAsync(int collectionId) =>
            _httpService.GetAsync<PeriodShipping?>($"collections/{collectionId}/frais-de-port");
    }
}
