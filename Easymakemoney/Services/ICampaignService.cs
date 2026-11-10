using Newtonsoft.Json.Linq;

namespace Easymakemoney.Services
{
    /// <summary>Campagnes de publicité (« gestion/campagnes »), contrat docs/publicite-gestion.md du backend.</summary>
    public interface ICampaignService
    {
        /// <summary>Une page de campagnes, les plus récentes d'abord.</summary>
        Task<List<Campaign>> ListAsync(int page, int limit);
        Task<Campaign> GetAsync(int id);
        Task<Campaign> CreateAsync(JObject body);
        Task<Campaign> UpdateAsync(int id, JObject body);
        Task DeleteAsync(int id);
        Task<CampaignProfitability> GetProfitabilityAsync(int id);
    }
}
