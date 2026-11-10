using System.Globalization;
using Easymakemoney.Utilities;

namespace Easymakemoney.Models
{
    /// <summary>
    /// Rentabilité perçue d'une campagne (« GET gestion/campagnes/{id}/rentabilite »). Estimation avant / après,
    /// montants en centimes dans la devise du site (<see cref="currency"/>).
    /// </summary>
    public sealed class CampaignProfitability
    {
        public CampaignRef campaign { get; set; } = new();
        public bool estimate { get; set; } = true;
        public string? method { get; set; }
        public string? currency { get; set; }
        public CampaignGroup group { get; set; } = new();
        public CampaignWindow window { get; set; } = new();
        public CampaignWindow reference { get; set; } = new();
        public CampaignScope scope { get; set; } = new();
        public CampaignSalesMetrics during { get; set; } = new();
        public CampaignSalesMetrics before { get; set; } = new();
        public CampaignSalesMetrics? lastYear { get; set; }
        public CampaignUplift uplift { get; set; } = new();
        public CampaignGainPerDollar? gainPerDollar { get; set; }
        public List<string> caveats { get; set; } = new();
    }

    public sealed class CampaignRef
    {
        public int id { get; set; }
        public string? name { get; set; }
    }

    public sealed class CampaignGroup
    {
        public List<int> campaignIds { get; set; } = new();
        public long amount { get; set; }
        public string? from { get; set; }
        public string? to { get; set; }
        public bool shared { get; set; }
    }

    public sealed class CampaignWindow
    {
        public string? from { get; set; }
        public string? to { get; set; }
        public int days { get; set; }
        public bool complete { get; set; }
    }

    public sealed class CampaignScope
    {
        public string? kind { get; set; }
        public List<int> productIds { get; set; } = new();
    }

    public sealed class CampaignSalesMetrics
    {
        public long revenue { get; set; }
        public long grossMargin { get; set; }
        public int orders { get; set; }
        public int items { get; set; }
        public long unknownCostRevenue { get; set; }
    }

    public sealed class CampaignUplift
    {
        public long revenue { get; set; }
        public long grossMargin { get; set; }
    }

    public sealed class CampaignGainPerDollar
    {
        public double revenue { get; set; }
        public double grossMargin { get; set; }
    }

    public enum CampaignGainLevel
    {
        /// <summary>Pas de jugement : campagne pas encore commencée, ou gain non calculé.</summary>
        Neutral,
        Good,
        Weak,
        Bad,
    }

    /// <summary>Une ligne « Avant », « Pendant » ou « L'an dernier » d'une mesure.</summary>
    public sealed class CampaignMetricRow
    {
        public CampaignMetricRow(string label, string value)
        {
            Label = label;
            Value = value;
        }

        public string Label { get; }
        public string Value { get; }
    }

    /// <summary>Une mesure (ventes, marge, commandes) comparée sur les fenêtres.</summary>
    public sealed class CampaignMetricBlock
    {
        public CampaignMetricBlock(string title, IReadOnlyList<CampaignMetricRow> rows)
        {
            Title = title;
            Rows = rows;
        }

        public string Title { get; }
        public IReadOnlyList<CampaignMetricRow> Rows { get; }
    }

    /// <summary>
    /// Textes et couleur de l'écran de rentabilité, calculés depuis la réponse du backend.
    /// Sans MAUI : testé dans Easymakemoney.Tests.
    /// </summary>
    public sealed class CampaignProfitabilityReport
    {
        public const string GoodColor = "#15803D";
        public const string WeakColor = "#B45309";
        public const string BadColor = "#B91C1C";
        public const string NeutralColor = "#475569";

        public CampaignProfitabilityReport(CampaignProfitability data, CultureInfo? culture = null)
        {
            culture ??= CultureInfo.CurrentCulture;
            var currency = string.IsNullOrWhiteSpace(data.currency) ? null : data.currency;
            string Money(long cents) => SiteCurrency.FormatCents(cents, culture, currency);
            string Signed(long cents) => cents > 0 ? $"+{Money(cents)}" : cents < 0 ? $"−{Money(-cents)}" : Money(0);

            IsNotStarted = data.window.days <= 0;
            IsRunning = !IsNotStarted && !data.window.complete;
            Level = LevelFor(data.gainPerDollar?.grossMargin, IsNotStarted);
            GainColor = ColorFor(Level);

            GainText = IsNotStarted || data.gainPerDollar == null
                ? "—"
                : FormatGain(data.gainPerDollar.grossMargin, culture, currency);
            GainCaption = $"de marge gagnée pour {FormatGain(1, culture, currency)} investi";
            GainVerdict = VerdictFor(Level, data.gainPerDollar == null);
            RevenueGainText = !IsNotStarted && data.gainPerDollar != null
                ? $"Ventes : {FormatGain(data.gainPerDollar.revenue, culture, currency)} pour {FormatGain(1, culture, currency)} investi"
                : string.Empty;

            StatusText = IsNotStarted
                ? "Pas encore commencée : la mesure démarrera avec la campagne."
                : IsRunning
                    ? $"Campagne en cours : mesure provisoire, arrêtée au {CampaignDates.ShortText(data.window.to, culture)}."
                    : string.Empty;
            HasStatus = StatusText.Length > 0;

            WindowText = IsNotStarted
                ? string.Empty
                : $"Pendant : du {CampaignDates.ShortText(data.window.from, culture)} au {CampaignDates.ShortText(data.window.to, culture)} ({DaysText(data.window.days)})";
            ReferenceText = IsNotStarted || string.IsNullOrWhiteSpace(data.reference.from)
                ? string.Empty
                : $"Avant : du {CampaignDates.ShortText(data.reference.from, culture)} au {CampaignDates.ShortText(data.reference.to, culture)}";

            IsShared = data.group.shared;
            SharedText = IsShared
                ? $"Mesurée avec d'autres campagnes ({data.group.campaignIds.Count} au total, {Money(data.group.amount)} investis ensemble)."
                : string.Empty;

            ScopeText = data.scope.kind == "targeted"
                ? data.scope.productIds.Count == 1 ? "Ventes mesurées : 1 article ciblé" : $"Ventes mesurées : {data.scope.productIds.Count} articles ciblés"
                : "Ventes mesurées : tout le magasin";

            HasLastYear = data.lastYear != null;
            Metrics = new[]
            {
                Block("Ventes", data, m => Money(m.revenue)),
                Block("Marge", data, m => Money(m.grossMargin)),
                Block("Commandes", data, m => m.orders.ToString(culture)),
            };

            RevenueUpliftText = Signed(data.uplift.revenue);
            MarginUpliftText = Signed(data.uplift.grossMargin);

            Caveats = data.caveats.Where(c => !string.IsNullOrWhiteSpace(c)).ToList();
            CaveatsText = string.Join("\n", Caveats.Select(c => $"• {c}"));
            HasCaveats = Caveats.Count > 0;
        }

        public CampaignGainLevel Level { get; }
        public string GainColor { get; }
        public string GainText { get; }
        public string GainCaption { get; }
        public string GainVerdict { get; }
        public string RevenueGainText { get; }
        public bool HasRevenueGain => RevenueGainText.Length > 0;
        public bool IsNotStarted { get; }
        public bool IsRunning { get; }
        public string StatusText { get; }
        public bool HasStatus { get; }
        public string WindowText { get; }
        public string ReferenceText { get; }
        public bool IsShared { get; }
        public string SharedText { get; }
        public string ScopeText { get; }
        public bool HasLastYear { get; }
        public IReadOnlyList<CampaignMetricBlock> Metrics { get; }
        public string RevenueUpliftText { get; }
        public string MarginUpliftText { get; }
        public IReadOnlyList<string> Caveats { get; }
        public string CaveatsText { get; }
        public bool HasCaveats { get; }

        /// <summary>Vert au-delà de 1, orange entre 0 et 1, rouge à 0 ou en dessous ; neutre sans mesure.</summary>
        public static CampaignGainLevel LevelFor(double? grossMarginPerDollar, bool notStarted = false)
        {
            if (notStarted || grossMarginPerDollar == null)
                return CampaignGainLevel.Neutral;
            if (grossMarginPerDollar.Value > 1)
                return CampaignGainLevel.Good;
            if (grossMarginPerDollar.Value > 0)
                return CampaignGainLevel.Weak;
            return CampaignGainLevel.Bad;
        }

        public static string ColorFor(CampaignGainLevel level) => level switch
        {
            CampaignGainLevel.Good => GoodColor,
            CampaignGainLevel.Weak => WeakColor,
            CampaignGainLevel.Bad => BadColor,
            _ => NeutralColor,
        };

        public static string VerdictFor(CampaignGainLevel level, bool noAmount = false) => level switch
        {
            CampaignGainLevel.Good => "La campagne a rapporté plus de marge qu'elle n'a coûté.",
            CampaignGainLevel.Weak => "La hausse de marge ne couvre pas encore le coût de la campagne.",
            CampaignGainLevel.Bad => "Aucune hausse de marge mesurée : la campagne n'est pas rentable à ce jour.",
            _ when noAmount => "Montant nul : le gain par dollar investi ne peut pas être calculé.",
            _ => "Pas encore de mesure.",
        };

        /// <summary>Gain pour une unité de la devise du site : 1.85 → « 1,85 $ ».</summary>
        public static string FormatGain(double value, CultureInfo culture, string? currency) =>
            SiteCurrency.FormatCents(Math.Round(value * 100, MidpointRounding.AwayFromZero), culture, currency);

        private static string DaysText(int days) => days == 1 ? "1 jour" : $"{days} jours";

        private static CampaignMetricBlock Block(string title, CampaignProfitability data, Func<CampaignSalesMetrics, string> value)
        {
            var rows = new List<CampaignMetricRow>
            {
                new("Avant", value(data.before)),
                new("Pendant", value(data.during)),
            };
            if (data.lastYear != null)
                rows.Add(new CampaignMetricRow("L'an dernier", value(data.lastYear)));
            return new CampaignMetricBlock(title, rows);
        }
    }
}
