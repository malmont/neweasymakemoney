using System.Globalization;
using Easymakemoney.Utilities;

namespace Easymakemoney.Models
{
    /// <summary>Projection de trésorerie (GET gestion/tresorerie, lot backend B1b-1, docs/tresorerie-gestion.md).</summary>
    public class TreasuryOverview
    {
        public string? currency { get; set; }
        public TreasurySettings settings { get; set; } = new();
        public TreasuryBalance? balance { get; set; }
        public TreasuryReceipts? estimatedDailyReceipts { get; set; }
        public string? alertSeries { get; set; }
        public TreasurySeries? projection { get; set; }
        public TreasurySeries? projectionWithSales { get; set; }
        public string? status { get; set; }
        public List<TreasuryCategory> byCategory { get; set; } = new();
        public List<TreasuryCaveat> caveats { get; set; } = new();

        public bool HasBalance => balance != null && status != "no_balance";

        /// <summary>Série qui sert à l'alerte : avec ou sans les ventes estimées, selon le réglage.</summary>
        public TreasurySeries? AlertSeries => alertSeries == "projectionWithSales" ? projectionWithSales : projection;

        public bool UsesEstimatedSales => alertSeries == "projectionWithSales";

        public string StatusText => status switch
        {
            "critical" => "Trésorerie critique",
            "warning" => "Sous votre seuil d'alerte",
            "ok" => "Trésorerie suffisante",
            _ => "Aucun solde saisi",
        };

        public string StatusDetail => status switch
        {
            "critical" => "Le solde prévu passe sous zéro dans les 14 prochains jours.",
            "warning" => "Le solde prévu passe sous votre seuil dans les 14 prochains jours.",
            "ok" => "Le solde prévu reste au-dessus de votre seuil sur 14 jours.",
            _ => "Saisissez le solde de votre compte en banque pour voir la projection.",
        };

        public Microsoft.Maui.Graphics.Color StatusColor => Microsoft.Maui.Graphics.Color.FromArgb(status switch
        {
            "critical" => "#B91C1C",
            "warning" => "#B45309",
            "ok" => "#047857",
            _ => "#64748B",
        });

        public string BalanceText => balance == null
            ? string.Empty
            : $"{Money(balance.amount)} au {TreasuryDates.Short(balance.date)}";

        public string Low14Text => AlertSeries?.low14 is { } low ? $"{Money(low.amount)} le {TreasuryDates.Short(low.date)}" : "—";
        public string LowText => AlertSeries?.low is { } low ? $"{Money(low.amount)} le {TreasuryDates.Short(low.date)}" : "—";
        public string EndText => AlertSeries?.end is { } end ? $"{Money(end.amount)} le {TreasuryDates.Short(end.date)}" : "—";

        public bool HasBelowThreshold => AlertSeries?.belowThresholdFrom != null;
        public string BelowThresholdText => HasBelowThreshold
            ? $"Sous le seuil à partir du {TreasuryDates.Short(AlertSeries!.belowThresholdFrom)}"
            : string.Empty;

        public string ThresholdText => $"Seuil d'alerte : {Money(settings.threshold)}";

        public string ReceiptsText => estimatedDailyReceipts is { } receipts
            ? $"Ventes encaissées : {Money(receipts.amount)} par jour en moyenne sur {receipts.windowDays} jours"
            : string.Empty;

        public string CaveatsText => string.Join("\n", caveats.Where(caveat => !string.IsNullOrWhiteSpace(caveat.text)).Select(caveat => $"• {caveat.text}"));
        public bool HasCaveats => CaveatsText.Length > 0;

        /// <summary>Prochaines échéances de la série d'alerte : jours avec des entrées ou des sorties, à partir d'aujourd'hui.</summary>
        public List<TreasuryDay> UpcomingDays(DateTime today, int count = 8) =>
            (AlertSeries?.days ?? new())
                .Where(day => day.opening != true && (day.In != 0 || day.Out != 0)
                              && TreasuryDates.Parse(day.date) is { } date && date >= today.Date)
                .Take(count)
                .Select(day => day.WithCurrency(currency))
                .ToList();

        public string Money(long cents) => SiteCurrency.FormatCents(cents, null, currency);
    }

    public class TreasurySettings
    {
        public long threshold { get; set; }
        public bool includeEstimatedSales { get; set; }
    }

    public class TreasuryBalance
    {
        public int id { get; set; }
        public string? date { get; set; }
        public long amount { get; set; }
        public string? note { get; set; }
        public int ageDays { get; set; }

        [JsonIgnore]
        public string? Currency { get; set; }

        public string DateText => TreasuryDates.Short(date);
        public string AmountText => SiteCurrency.FormatCents(amount, null, Currency);
        public bool HasNote => !string.IsNullOrWhiteSpace(note);
    }

    public class TreasuryReceipts
    {
        public long amount { get; set; }
        public int windowDays { get; set; }
        public int orders { get; set; }
    }

    public class TreasurySeries
    {
        public List<TreasuryDay> days { get; set; } = new();
        public TreasuryPoint? low { get; set; }
        public TreasuryPoint? low14 { get; set; }
        public string? belowThresholdFrom { get; set; }
        public TreasuryPoint? end { get; set; }
    }

    public class TreasuryPoint
    {
        public string? date { get; set; }
        public long amount { get; set; }
    }

    public class TreasuryDay
    {
        public string? date { get; set; }

        [JsonProperty("in")]
        public long In { get; set; }

        [JsonProperty("out")]
        public long Out { get; set; }

        public long? receipts { get; set; }
        public bool? receiptsEstimated { get; set; }
        public long balance { get; set; }
        public bool? opening { get; set; }

        [JsonIgnore]
        public string? Currency { get; set; }

        public TreasuryDay WithCurrency(string? currency)
        {
            Currency = currency;
            return this;
        }

        public string DateText => TreasuryDates.Short(date);
        public string MovesText => string.Join("  ", new[]
        {
            In != 0 ? $"+ {SiteCurrency.FormatCents(In, null, Currency)}" : null,
            Out != 0 ? $"− {SiteCurrency.FormatCents(Out, null, Currency)}" : null,
        }.Where(text => text != null));
        public string BalanceText => SiteCurrency.FormatCents(balance, null, Currency);
        public bool IsNegative => balance < 0;
    }

    public class TreasuryCategory
    {
        public string? category { get; set; }
        public string? label { get; set; }

        [JsonProperty("in")]
        public long In { get; set; }

        [JsonProperty("out")]
        public long Out { get; set; }
    }

    public class TreasuryCaveat
    {
        public string? code { get; set; }
        public string? text { get; set; }
    }

    /// <summary>Mouvement prévu (loyer, salaires, prêt…), saisi à la main.</summary>
    public class TreasuryMovement
    {
        public int? id { get; set; }
        public bool grouped { get; set; }
        public string? label { get; set; }
        public string? category { get; set; }
        public string? categoryLabel { get; set; }
        public string? direction { get; set; }
        public long amount { get; set; }
        public string? startDate { get; set; }
        public string? recurrence { get; set; }
        public string? endDate { get; set; }
        public string? notes { get; set; }
        public string? nextDate { get; set; }

        [JsonIgnore]
        public string? Currency { get; set; }

        public bool IsIncoming => direction == "in";
        public bool CanDelete => id.HasValue && !grouped;

        public string AmountText => $"{(IsIncoming ? "+" : "−")} {SiteCurrency.FormatCents(amount, null, Currency)}";

        public string RecurrenceText => TreasuryChoices.RecurrenceLabel(recurrence);

        public string ScheduleText
        {
            get
            {
                var parts = new List<string> { $"{categoryLabel ?? category} · {RecurrenceText}" };
                if (nextDate != null)
                    parts.Add($"prochaine fois le {TreasuryDates.Short(nextDate)}");
                else
                    parts.Add("terminé");
                return string.Join(" · ", parts);
            }
        }
    }

    public sealed class TreasuryChoice
    {
        public TreasuryChoice(string code, string label)
        {
            Code = code;
            Label = label;
        }

        public string Code { get; }
        public string Label { get; }
        public override string ToString() => Label;
    }

    public static class TreasuryChoices
    {
        public static IReadOnlyList<TreasuryChoice> Categories { get; } = new[]
        {
            new TreasuryChoice("rent", "Loyer"),
            new TreasuryChoice("salaries", "Salaires"),
            new TreasuryChoice("insurance", "Assurances"),
            new TreasuryChoice("subscription", "Abonnements"),
            new TreasuryChoice("loan", "Prêt"),
            new TreasuryChoice("tax", "Taxes et impôts"),
            new TreasuryChoice("other", "Autres"),
        };

        public static IReadOnlyList<TreasuryChoice> Recurrences { get; } = new[]
        {
            new TreasuryChoice("none", "Une seule fois"),
            new TreasuryChoice("weekly", "Chaque semaine"),
            new TreasuryChoice("monthly", "Chaque mois"),
            new TreasuryChoice("quarterly", "Chaque trimestre"),
            new TreasuryChoice("yearly", "Chaque année"),
        };

        public static IReadOnlyList<TreasuryChoice> Directions { get; } = new[]
        {
            new TreasuryChoice("out", "Sortie (je paie)"),
            new TreasuryChoice("in", "Entrée (je reçois)"),
        };

        public static string RecurrenceLabel(string? code) =>
            Recurrences.FirstOrDefault(choice => choice.Code == code)?.Label ?? "Une seule fois";
    }

    public static class TreasuryDates
    {
        public static DateTime? Parse(string? isoDate) =>
            DateTime.TryParseExact(isoDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) ? date : null;

        public static string Short(string? isoDate) =>
            Parse(isoDate) is { } date ? date.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) : isoDate ?? string.Empty;

        public static string Iso(DateTime date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }
}
