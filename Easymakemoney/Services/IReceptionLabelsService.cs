using Easymakemoney.Services.Api;

namespace Easymakemoney.Services
{
    /// <summary>Étiquettes d'une commande reçue (backend CB-2) : lecture, génération des codes manquants, PDF.</summary>
    public interface IReceptionLabelsService
    {
        Task<ReceptionLabels> GetAsync(int commandeId);
        /// <summary>Génère les codes de toutes les variantes de « missing » ; ne remplace jamais un code ; rend la liste à jour.</summary>
        Task<ReceptionLabels> GenerateMissingAsync(int commandeId);
        /// <summary>format : « a4-50x30 » (planche A4, par défaut) ou « roll-50x30 » (rouleau thermique, 200 au plus).</summary>
        Task<DownloadedFile> PdfAsync(int commandeId, IEnumerable<(int VariantId, int Copies)> labels, bool showPrice, string format = "a4-50x30");
    }

    public class ReceptionLabelsService : IReceptionLabelsService
    {
        private readonly IHttpService _httpService;

        public ReceptionLabelsService(IHttpService httpService)
        {
            _httpService = httpService;
        }

        public async Task<ReceptionLabels> GetAsync(int commandeId) =>
            await _httpService.GetAsync<ReceptionLabels>($"commandes/{commandeId}/etiquettes?locale=fr") ?? new ReceptionLabels();

        public async Task<ReceptionLabels> GenerateMissingAsync(int commandeId) =>
            await _httpService.PostAsyncWithAuth<ReceptionLabels>($"commandes/{commandeId}/etiquettes/codes?locale=fr", new { }) ?? new ReceptionLabels();

        public Task<DownloadedFile> PdfAsync(int commandeId, IEnumerable<(int VariantId, int Copies)> labels, bool showPrice, string format = "a4-50x30") =>
            _httpService.PostForFileAsync($"commandes/{commandeId}/etiquettes/pdf?locale=fr", new Dictionary<string, object>
            {
                ["labels"] = labels.Select(l => new Dictionary<string, object> { ["variantId"] = l.VariantId, ["copies"] = l.Copies }).ToList(),
                ["showPrice"] = showPrice,
                ["format"] = format,
            });
    }
}
