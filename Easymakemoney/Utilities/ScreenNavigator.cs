using Newtonsoft.Json.Linq;

namespace Easymakemoney.Utilities
{
    /// <summary>
    /// Traduit un code d'écran du backend (vignettes d'accueil, conseils du guide) en navigation dans l'application.
    /// Un code inconnu ne fait rien : la carte reste informative.
    /// </summary>
    public static class ScreenNavigator
    {
        public static async Task<bool> OpenAsync(string? screen, JToken? parameters, JToken? figures = null,
            string? title = null, string? intro = null, string? currency = null, HomeTile? tile = null)
        {
            if (Shell.Current is not { } shell || string.IsNullOrWhiteSpace(screen))
                return false;

            var values = parameters as JObject;
            int Int(string name) => values?.Value<int?>(name) ?? 0;

            switch (screen)
            {
                case "documents.scan":
                    var type = values?.Value<string>("type") is "facture" ? "facture" : "note";
                    await shell.GoToAsync($"{nameof(DocumentReadPage)}?Type={type}");
                    return true;

                case "campagnes.new":
                    var from = Int("fromCampaignId");
                    await shell.GoToAsync(from > 0 ? $"{nameof(CampaignFormPage)}?FromCampaignId={from}" : nameof(CampaignFormPage));
                    return true;

                case "stock.low" when tile != null:
                    await shell.GoToAsync(nameof(StockLowPage), new ShellNavigationQueryParameters { ["Tile"] = tile });
                    return true;

                case "stock.low":
                case "articles":
                    var items = figures is JArray array ? array.ToObject<List<LowStockItem>>() ?? new() : new();
                    foreach (var item in items)
                        item.Currency = currency;
                    await shell.GoToAsync(nameof(StockLowPage), new ShellNavigationQueryParameters
                    {
                        ["Items"] = items,
                        ["Title"] = title ?? "Articles",
                        ["Intro"] = intro ?? string.Empty,
                    });
                    return true;

                case "periode.budget" when Int("periodId") > 0:
                    await shell.GoToAsync($"{nameof(PeriodBudgetPage)}?PeriodId={Int("periodId")}");
                    return true;

                case "locations":
                    // Conseils de location (RENT1 à RENT3) : l'écran des statistiques de location.
                    await shell.GoToAsync(nameof(RentalStatisticsPage));
                    return true;

                case "tresorerie":
                    await shell.GoToAsync(nameof(TreasuryPage));
                    return true;

                case "voyages.suivi" when Int("tripId") > 0:
                    await shell.GoToAsync($"{nameof(TripFollowUpPage)}?TripId={Int("tripId")}");
                    return true;

                case "conseils":
                    var adviceId = Int("adviceId");
                    await shell.GoToAsync(adviceId > 0 ? $"{nameof(AdvicePage)}?AdviceId={adviceId}" : nameof(AdvicePage));
                    return true;

                case "simulateur" when Int("periodId") > 0:
                    var query = new ShellNavigationQueryParameters { ["PeriodId"] = Int("periodId") };
                    if (values?["promotion"] is JObject promotion && promotion.Value<double?>("percent") is { } percent && percent > 0)
                    {
                        query["PromotionPercent"] = percent;
                        query["ProductIds"] = promotion["productIds"] is JArray ids ? ids.Values<int>().ToList() : new List<int>();
                    }
                    await shell.GoToAsync(nameof(PeriodSimulationPage), query);
                    return true;

                default:
                    return false;
            }
        }
    }
}
