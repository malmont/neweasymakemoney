using Newtonsoft.Json.Linq;

namespace Easymakemoney.Services
{
    /// <summary>Appartenance du site à un groupe de boutiques (GET /api/group), lue au plus juste : group est-il nul ?</summary>
    public interface IGroupService
    {
        /// <summary>Vrai si le site fait partie d'un groupe, siège compris.</summary>
        Task<bool> IsInGroupAsync();
    }

    public class GroupService : IGroupService
    {
        private readonly IHttpService _httpService;

        public GroupService(IHttpService httpService)
        {
            _httpService = httpService;
        }

        public async Task<bool> IsInGroupAsync() =>
            GroupMembership.IsMember(await _httpService.GetAsync<JObject?>("group"));
    }

    /// <summary>Transferts de stock que la boutique SOURCE doit envoyer (lot backend G-5, contrat § 10.2).</summary>
    public interface IGroupTransferService
    {
        /// <summary>GET /api/site/group-transfers → { transfers: [...] } (états requested, sending, send_failed ; lignes sans coût).</summary>
        Task<List<GroupTransfer>> GetTransfersToSendAsync();

        /// <summary>POST /api/site/group-transfers/{id}/send : le stock sort de la boutique, la commande de la destination est créée.</summary>
        Task SendAsync(int transferId);
    }

    public class GroupTransferService : IGroupTransferService
    {
        private readonly IHttpService _httpService;

        public GroupTransferService(IHttpService httpService)
        {
            _httpService = httpService;
        }

        public async Task<List<GroupTransfer>> GetTransfersToSendAsync() =>
            GroupTransfer.ReadList(await _httpService.GetAsync<JObject?>("site/group-transfers"));

        public async Task SendAsync(int transferId) =>
            await _httpService.PostAsyncWithAuth<JObject?>($"site/group-transfers/{transferId}/send", new { });
    }
}
