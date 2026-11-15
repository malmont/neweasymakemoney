using Easymakemoney.Services.Api;

namespace Easymakemoney.Services
{
    /// <summary>Codes-barres du catalogue (GET|POST|PUT /api/gestion/codes-barres…, backend CB-3) ; textes en français.</summary>
    public interface ICatalogBarcodeService
    {
        Task<BarcodeSearch> SearchAsync(string code);
        Task<BarcodeMissing> GetMissingAsync();
        /// <summary>Un lancement : au plus 500 codes ; relancer tant que remaining &gt; 0 et written &gt; 0.</summary>
        Task<BarcodeGenerateMissing> GenerateMissingAsync();
        Task<BarcodeGenerated> GenerateAsync(bool variant, int id);
        /// <summary>Pose (ou retire, barcode vide) un code ; replace est exigé pour changer ou retirer un code déjà posé.</summary>
        Task<BarcodeWrite> SetAsync(bool variant, int id, string? barcode, bool replace);
        Task<DownloadedFile> LabelsPdfAsync(IEnumerable<BarcodeLabelRequest> labels, bool showPrice, string format = "a4-50x30");
    }

    public class CatalogBarcodeService : ICatalogBarcodeService
    {
        private const string Base = "gestion/codes-barres";
        private const string Locale = "locale=fr";
        private readonly IHttpService _httpService;

        public CatalogBarcodeService(IHttpService httpService)
        {
            _httpService = httpService;
        }

        private static string Kind(bool variant) => variant ? "variantes" : "articles";

        public async Task<BarcodeSearch> SearchAsync(string code) =>
            await _httpService.GetAsync<BarcodeSearch>($"{Base}/recherche?code={Uri.EscapeDataString(code.Trim())}&{Locale}") ?? new BarcodeSearch { code = code };

        public async Task<BarcodeMissing> GetMissingAsync() =>
            await _httpService.GetAsync<BarcodeMissing>($"{Base}/manquants?{Locale}") ?? new BarcodeMissing();

        public async Task<BarcodeGenerateMissing> GenerateMissingAsync() =>
            await _httpService.PostAsyncWithAuth<BarcodeGenerateMissing>($"{Base}/manquants/generer?{Locale}", new { }) ?? new BarcodeGenerateMissing();

        public async Task<BarcodeGenerated> GenerateAsync(bool variant, int id) =>
            await _httpService.PostAsyncWithAuth<BarcodeGenerated>($"{Base}/{Kind(variant)}/{id}/generer?{Locale}", new { }) ?? new BarcodeGenerated();

        public async Task<BarcodeWrite> SetAsync(bool variant, int id, string? barcode, bool replace)
        {
            var body = new Dictionary<string, object?> { ["barcode"] = string.IsNullOrWhiteSpace(barcode) ? null : barcode.Trim() };
            if (replace)
                body["replace"] = true;
            return await _httpService.PutAsyncWithAuth<BarcodeWrite>($"{Base}/{Kind(variant)}/{id}?{Locale}", body) ?? new BarcodeWrite();
        }

        public Task<DownloadedFile> LabelsPdfAsync(IEnumerable<BarcodeLabelRequest> labels, bool showPrice, string format = "a4-50x30")
        {
            var lines = labels.Select(l =>
            {
                var line = new Dictionary<string, object> { ["copies"] = l.Copies };
                if (l.Sheet.IsVariant)
                    line["variantId"] = l.Sheet.variantId!.Value;
                else
                    line["productId"] = l.Sheet.productId;
                return line;
            }).ToList();
            return _httpService.PostForFileAsync($"{Base}/etiquettes/pdf?{Locale}", new Dictionary<string, object> { ["labels"] = lines, ["showPrice"] = showPrice, ["format"] = format });
        }
    }
}
