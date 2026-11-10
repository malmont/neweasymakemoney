using System.Globalization;
using Easymakemoney.Utilities;

namespace Easymakemoney.Models
{
    /// <summary>
    /// Campagne de publicité (« GET gestion/campagnes »). Montants en centimes dans la devise de la campagne
    /// (<see cref="currency"/>) ; <see cref="amountSite"/> est le même montant ramené dans la devise du site.
    /// </summary>
    public sealed class Campaign
    {
        public int id { get; set; }
        public string name { get; set; } = string.Empty;
        public string? channel { get; set; }
        public string? startDate { get; set; }
        public string? endDate { get; set; }
        public long amount { get; set; }
        public string? currency { get; set; }
        public double? exchangeRate { get; set; }
        public long? amountSite { get; set; }
        public string? siteCurrency { get; set; }
        public List<int> productIds { get; set; } = new();
        public List<int> commandeIds { get; set; } = new();
        public string? notes { get; set; }
        public CampaignDocument? document { get; set; }
        public CampaignPeriod? period { get; set; }
        public string? createdAt { get; set; }

        public string ChannelText => CampaignChannels.LabelFor(channel);

        public string DatesText => DatesTextFor(CultureInfo.CurrentCulture);

        public string AmountText => AmountTextFor(CultureInfo.CurrentCulture);

        public bool IsForeign => !CampaignChannels.SameCurrency(currency, siteCurrency);

        /// <summary>Montant ramené dans la devise du site, affiché sous le montant d'une campagne en devise étrangère.</summary>
        public string AmountSiteText => IsForeign && amountSite.HasValue
            ? $"soit {SiteCurrency.FormatCents(amountSite.Value, CultureInfo.CurrentCulture, siteCurrency)}"
            : string.Empty;

        public bool HasAmountSite => AmountSiteText.Length > 0;

        public string PeriodText => string.IsNullOrWhiteSpace(period?.name) ? "Sans période" : $"Période : {period!.name}";

        public bool HasTargets => productIds.Count > 0 || commandeIds.Count > 0;

        public string TargetText => productIds.Count switch
        {
            0 when commandeIds.Count == 0 => "Tout le magasin",
            0 => "Articles de commandes choisies",
            1 => "1 article ciblé",
            var n => $"{n} articles ciblés",
        };

        public string DatesTextFor(CultureInfo culture)
        {
            var start = CampaignDates.Parse(startDate);
            var end = CampaignDates.Parse(endDate);
            if (start == null)
                return string.Empty;
            if (end == null || end.Value.Date == start.Value.Date)
                return $"Le {CampaignDates.Short(start.Value, culture)}";
            return $"Du {CampaignDates.Short(start.Value, culture)} au {CampaignDates.Short(end.Value, culture)}";
        }

        public string AmountTextFor(CultureInfo culture) =>
            SiteCurrency.FormatCents(amount, culture, string.IsNullOrWhiteSpace(currency) ? siteCurrency : currency);
    }

    public sealed class CampaignDocument
    {
        public int id { get; set; }
        public string? type { get; set; }
        public string? invoiceNumber { get; set; }
        public string? invoiceDate { get; set; }
    }

    public sealed class CampaignPeriod
    {
        public int id { get; set; }
        public string? periodKey { get; set; }
        public string? name { get; set; }
    }

    /// <summary>Un canal dans le sélecteur du formulaire.</summary>
    public sealed class CampaignChannelChoice
    {
        public CampaignChannelChoice(string code, string label)
        {
            Code = code;
            Label = label;
        }

        public string Code { get; }
        public string Label { get; }
    }

    /// <summary>Canaux connus du backend et leurs libellés en français.</summary>
    public static class CampaignChannels
    {
        public const string Other = "other";

        public static IReadOnlyList<CampaignChannelChoice> All { get; } = new[]
        {
            new CampaignChannelChoice("facebook", "Facebook"),
            new CampaignChannelChoice("instagram", "Instagram"),
            new CampaignChannelChoice("google", "Google"),
            new CampaignChannelChoice("tiktok", "TikTok"),
            new CampaignChannelChoice("email", "E-mail"),
            new CampaignChannelChoice("print", "Imprimé"),
            new CampaignChannelChoice("radio", "Radio"),
            new CampaignChannelChoice("flyer", "Flyers"),
            new CampaignChannelChoice("influence", "Influence"),
            new CampaignChannelChoice("event", "Événement"),
            new CampaignChannelChoice(Other, "Autre"),
        };

        public static bool IsKnown(string? code) => All.Any(c => c.Code == code);

        public static CampaignChannelChoice? Find(string? code) => All.FirstOrDefault(c => c.Code == code);

        public static string LabelFor(string? code) => Find(code)?.Label ?? "Autre";

        /// <summary>
        /// Canal probable d'après le nom du commerçant lu sur la facture (« Meta Platforms » : Facebook) ;
        /// null quand rien ne permet de deviner.
        /// </summary>
        public static string? Guess(string? merchant)
        {
            if (string.IsNullOrWhiteSpace(merchant))
                return null;

            var text = merchant.ToLowerInvariant();
            if (text.Contains("instagram")) return "instagram";
            if (text.Contains("facebook") || text.Contains("meta ")) return "facebook";
            if (text.Contains("google") || text.Contains("youtube")) return "google";
            if (text.Contains("tiktok") || text.Contains("bytedance")) return "tiktok";
            return null;
        }

        public static bool SameCurrency(string? a, string? b) =>
            string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b)
            || string.Equals(a.Trim(), b.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Dates du contrat : « yyyy-MM-dd ».</summary>
    public static class CampaignDates
    {
        public const string Format = "yyyy-MM-dd";

        public static DateTime? Parse(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;
            if (DateTime.TryParseExact(text.Length >= 10 ? text[..10] : text, Format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                return date;
            return null;
        }

        public static string ToApi(DateTime date) => date.ToString(Format, CultureInfo.InvariantCulture);

        /// <summary>Toujours « 24/12/2026 » : la culture fr-CA mettrait des tirets.</summary>
        public static string Short(DateTime date, CultureInfo culture) => date.ToString("dd'/'MM'/'yyyy", culture);

        public static string ShortText(string? text, CultureInfo culture) =>
            Parse(text) is { } date ? Short(date, culture) : string.Empty;
    }
}
