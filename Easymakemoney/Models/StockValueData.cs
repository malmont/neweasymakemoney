using Newtonsoft.Json;

namespace Easymakemoney.Models
{
    public class StockValueData
    {
        [JsonProperty("month")]
        public string Month { get; set; }

        /// <summary>En unités de monnaie (dollars), pas en cents : le backend divise déjà par 100 (ProductRepository::calculateCurrentStockValue).</summary>
        [JsonProperty("stock_value")]
        public double StockValue { get; set; }

        /// <summary>En cents, si le backend l'envoie (demandé) ; sinon calculé depuis stock_value.</summary>
        [JsonProperty("stock_value_cents")]
        public long? StockValueCentsFromServer { get; set; }

        /// <summary>Valeur à afficher avec le convertisseur de monnaie, qui attend des cents.</summary>
        [JsonIgnore]
        public long StockValueCents => StockValueCentsFromServer ?? (long)Math.Round(StockValue * 100);

        /// <summary>Mois en français (« novembre 2026 ») : le backend l'envoie en anglais (« November 2026 »).</summary>
        [JsonIgnore]
        public string MonthText => DateTime.TryParseExact(Month, "MMMM yyyy", System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var date)
            ? date.ToString("MMMM yyyy", new System.Globalization.CultureInfo("fr-FR"))
            : Month;
    }
}
