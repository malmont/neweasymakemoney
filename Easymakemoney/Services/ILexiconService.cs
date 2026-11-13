using Easymakemoney.Utilities;

namespace Easymakemoney.Services
{
    /// <summary>
    /// W2-APPRENDRE : le lexique de la gestion servi par le serveur (GET /api/gestion/lexique), mêmes clés que le glossaire local.
    /// L'illustration d'une notion avec les chiffres du site est écrite par le serveur (GET /api/gestion/lexique/{key}) ; sans donnée, rien.
    /// </summary>
    public interface ILexiconService
    {
        /// <summary>Charge le lexique s'il a changé (version) ; une erreur garde le lexique en place, le glossaire local restant le secours.</summary>
        Task RefreshAsync();

        /// <summary>La phrase d'illustration du serveur pour une notion, avec les chiffres du site ; null sans donnée ou hors connexion.</summary>
        Task<string?> GetIllustrationAsync(string key);
    }

    public class LexiconResponse
    {
        public string? version { get; set; }
        public List<LexiconEntry> entries { get; set; } = new();
    }

    public class LexiconEntry
    {
        public string key { get; set; } = string.Empty;
        public string? term { get; set; }
        public string? definition { get; set; }
        public LexiconSite? site { get; set; }
    }

    public class LexiconSite
    {
        public string? illustration { get; set; }
    }

    public class LexiconService : ILexiconService
    {
        /// <summary>Le lexique change rarement : une lecture au plus toutes les 30 minutes.</summary>
        public static readonly TimeSpan RefreshInterval = TimeSpan.FromMinutes(30);

        private readonly IHttpService _httpService;
        private readonly Func<DateTime> _utcNow;
        private DateTime? _lastRefresh;

        public LexiconService(IHttpService httpService) : this(httpService, () => DateTime.UtcNow)
        {
        }

        public LexiconService(IHttpService httpService, Func<DateTime> utcNow)
        {
            _httpService = httpService;
            _utcNow = utcNow;
        }

        public async Task RefreshAsync()
        {
            var now = _utcNow();
            if (_lastRefresh is { } last && now - last < RefreshInterval)
                return;
            _lastRefresh = now;
            try
            {
                var lexicon = await _httpService.GetAsync<LexiconResponse>("gestion/lexique?locale=fr");
                if (lexicon?.entries is { Count: > 0 } entries && lexicon.version != Glossaire.ServerVersion)
                    Glossaire.ApplyServer(lexicon.version, entries.Select(e => (e.key, e.term, e.definition)));
            }
            catch (Exception e)
            {
                // Hors connexion ou module inactif : le glossaire local reste en place.
                System.Diagnostics.Debug.WriteLine($"Lexique indisponible : {e.Message}");
                _lastRefresh = null;
            }
        }

        public async Task<string?> GetIllustrationAsync(string key)
        {
            try
            {
                var entry = await _httpService.GetAsync<LexiconEntry>($"gestion/lexique/{Uri.EscapeDataString(key)}?locale=fr");
                return string.IsNullOrWhiteSpace(entry?.site?.illustration) ? null : entry!.site!.illustration!.Trim();
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine($"Illustration indisponible : {e.Message}");
                return null;
            }
        }
    }
}
