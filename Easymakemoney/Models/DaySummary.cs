using System.Globalization;
using Easymakemoney.Utilities;
using Newtonsoft.Json.Linq;

namespace Easymakemoney.Models
{
    /// <summary>
    /// Lot JOUR : « Ma journée », GET /api/gestion/journee. Tous les chiffres viennent du serveur (mêmes définitions que les statistiques
    /// « aujourd'hui », « À traiter », la trésorerie et la vignette de stock bas) ; l'appli ne fait que les afficher.
    /// </summary>
    public class DaySummary
    {
        public string? date { get; set; }
        public string? timezone { get; set; }
        public string? currency { get; set; }
        public string? generatedAt { get; set; }
        public DaySales? sales { get; set; }
        public DayReceipts? receipts { get; set; }
        public DayToShip? toShip { get; set; }
        public DaySupport? support { get; set; }
        public DayCash? cash { get; set; }
        public DayStock? stock { get; set; }
        public JObject? screens { get; set; }

        private string Money(long cents) => SiteCurrency.FormatCents(cents, currencyCode: currency);

        public string SalesText => sales is null
            ? "—"
            : $"{Money(sales.total)} · {sales.orders} commande{(sales.orders > 1 ? "s" : "")}";

        /// <summary>« +12,5 % par rapport à un jeudi habituel à 15 h » : comparaison calculée par le serveur.</summary>
        public string? SalesTrendText
        {
            get
            {
                var trend = sales?.vsSameWeekdayAverage;
                if (trend?.percent is not { } percent)
                    return null;
                var sign = percent > 0 ? "+" : percent < 0 ? "−" : string.Empty;
                var day = DateTimeOffset.TryParse(date, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
                    ? parsed.DateTime.ToString("dddd", CultureInfo.GetCultureInfo("fr-CA"))
                    : "jour";
                var at = HourText(trend.asOf) is { } hour ? $" à {hour}" : string.Empty;
                return $"{sign}{Math.Abs(percent).ToString("0.#", CultureInfo.GetCultureInfo("fr-CA"))} % par rapport à un {day} habituel{at}";
            }
        }

        /// <summary>« 15:02 » (heure du magasin, contrat JOUR) → « 15 h 02 » ; un texte déjà rédigé est gardé tel quel.</summary>
        public static string? HourText(string? asOf)
        {
            if (string.IsNullOrWhiteSpace(asOf))
                return null;
            var parts = asOf.Trim().Split(':');
            return parts.Length >= 2 && int.TryParse(parts[0], out var h) && int.TryParse(parts[1], out var m) && h is >= 0 and < 24 && m is >= 0 and < 60
                ? $"{h} h {m:00}"
                : asOf.Trim();
        }

        public bool HasSalesTrend => SalesTrendText != null;
        public bool IsSalesUp => sales?.vsSameWeekdayAverage?.percent is > 0;
        public bool IsSalesDown => sales?.vsSameWeekdayAverage?.percent is < 0;

        public string ToShipText => toShip is null || toShip.count == 0
            ? "Rien à expédier"
            : $"{toShip.count} commande{(toShip.count > 1 ? "s" : "")} à expédier";

        public bool HasToShip => toShip?.count > 0;

        public bool HasSupport => support != null;

        public string SupportText => support is null
            ? string.Empty
            : support.open == 0
                ? "Aucune demande en attente"
                : $"{support.open} demande{(support.open > 1 ? "s" : "")} en attente" + (support.overdue > 0 ? $", dont {support.overdue} en retard" : string.Empty);

        public bool IsSupportLate => support?.overdue > 0;

        public bool HasCash => cash != null;

        public string CashText => cash?.balance is { } balance ? $"Solde : {Money(balance)}" : string.Empty;

        public string? CashLowText => cash?.lowestIn14Days is { } low
            ? $"Point bas sur 14 jours : {Money(low.amount)}"
            : null;

        public bool IsCashWarning => cash?.state is "warning" or "critical";

        public string StockText => stock is null || stock.lowCount == 0
            ? "Aucun article en stock bas"
            : $"{stock.lowCount} article{(stock.lowCount > 1 ? "s" : "")} en stock bas";

        public bool HasLowStock => stock?.lowCount > 0;

        /// <summary>Code d'écran du serveur pour une rubrique (« statistiques », « tresorerie », « stock.low »…), ou null.</summary>
        public string? Screen(string part) => screens?[part]?.Type == JTokenType.String ? screens[part]!.Value<string>() : null;
    }

    public class DaySales
    {
        public long total { get; set; }
        public int orders { get; set; }
        public List<DayChannel> byChannel { get; set; } = new();
        public DayTrend? vsSameWeekdayAverage { get; set; }
    }

    public class DayChannel
    {
        public string channel { get; set; } = string.Empty;
        public long total { get; set; }
        public int orders { get; set; }
    }

    public class DayTrend
    {
        public double? percent { get; set; }
        public long average { get; set; }
        public int weeks { get; set; }
        public string? asOf { get; set; }
    }

    public class DayReceipts
    {
        public long total { get; set; }
    }

    public class DayToShip
    {
        public int count { get; set; }
        public string? oldestAt { get; set; }
    }

    public class DaySupport
    {
        public int open { get; set; }
        public int overdue { get; set; }
    }

    public class DayCash
    {
        public long? balance { get; set; }
        public string? balanceDate { get; set; }
        public DayCashLow? lowestIn14Days { get; set; }
        public string? state { get; set; }
    }

    public class DayCashLow
    {
        public long amount { get; set; }
        public string? date { get; set; }
    }

    public class DayStock
    {
        public int lowCount { get; set; }
    }
}
