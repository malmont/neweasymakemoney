using Newtonsoft.Json.Linq;

namespace Easymakemoney.Models
{
    /// <summary>Vignette d'accueil (GET gestion/accueil/vignettes, voir docs/vignettes-gestion.md du backend).</summary>
    public class HomeTile
    {
        public string id { get; set; } = string.Empty;
        public string? kind { get; set; }
        public int priority { get; set; }
        public string? title { get; set; }
        public string? text { get; set; }
        public HomeTileAction? action { get; set; }
        public List<HomeTileSource>? sources { get; set; }
        public bool dismissible { get; set; }

        /// <summary>Forme propre à chaque vignette (liste pour le stock bas) ; un objet vide PHP peut arriver en tableau.</summary>
        public JToken? figures { get; set; }

        public bool IsAlert => kind == "alert";
        public string KindText => IsAlert ? "Alerte" : "Premiers pas";
        public string Icon => IsAlert ? "M12 9v4M12 17h.01M10.3 3.9L1.8 18a2 2 0 001.7 3h17a2 2 0 001.7-3L13.7 3.9a2 2 0 00-3.4 0z" : "M9 18h6M10 22h4M12 2a7 7 0 00-4 12.7V17h8v-2.3A7 7 0 0012 2z";

        public string SourcesText => sources == null || sources.Count == 0
            ? string.Empty
            : string.Join(" · ", sources.Select(source => $"{source.label} : {source.value}"));

        public bool HasSources => SourcesText.Length > 0;

        public string? Screen => action?.screen;

        /// <summary>Paramètre texte de l'action (par exemple <c>type</c> pour documents.scan), nul si absent.</summary>
        public string? Param(string name) =>
            action?.@params is JObject parameters ? parameters.Value<string?>(name) : null;

        /// <summary>Lignes du stock bas (figures), vide pour les autres vignettes.</summary>
        public List<LowStockItem> LowStockItems() =>
            figures is JArray items ? items.ToObject<List<LowStockItem>>() ?? new() : new();
    }

    public class HomeTileAction
    {
        public string? screen { get; set; }

        [JsonProperty("params")]
        public JToken? @params { get; set; }
    }

    public class HomeTileSource
    {
        public string? label { get; set; }
        public JToken? value { get; set; }
    }

    public class LowStockItem
    {
        public int productId { get; set; }
        public int variantId { get; set; }
        public string? name { get; set; }
        public int stock { get; set; }
        public int soldLast30Days { get; set; }
        public double? daysLeft { get; set; }

        public string StockText => stock <= 0 ? "Rupture" : $"{stock} en stock";
        public string SoldText => $"{soldLast30Days} vendus en 30 jours";

        public string DaysLeftText => stock <= 0 || daysLeft is null or <= 0
            ? "À racheter maintenant"
            : daysLeft < 1 ? "Moins d'un jour de vente" : $"Environ {Math.Floor(daysLeft.Value)} jours de vente";
    }
}
