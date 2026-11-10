using System.Globalization;
using Newtonsoft.Json.Linq;

namespace Easymakemoney.Models
{
    /// <summary>
    /// Ce que la lecture d'une facture de régie (document de type note) apporte au formulaire de campagne.
    /// Sans MAUI : testé dans Easymakemoney.Tests.
    /// </summary>
    public sealed class CampaignDocumentPrefill
    {
        public string? AmountText { get; init; }
        public DateTime? Date { get; init; }
        public string? Name { get; init; }
        public string? Channel { get; init; }

        /// <summary>Devise lue, null si absente ou égale à celle du site.</summary>
        public string? Currency { get; init; }

        public List<string> Remarks { get; init; } = new();

        public static CampaignDocumentPrefill Read(JObject? extraction, string? siteCurrency, CultureInfo? culture = null)
        {
            culture ??= CultureInfo.CurrentCulture;

            var amount = extraction?["amount"]?.Type == JTokenType.Integer ? extraction["amount"]!.Value<long?>() : null;
            var merchant = extraction?["merchant"]?.Type == JTokenType.String ? extraction["merchant"]!.Value<string>()?.Trim() : null;
            var description = extraction?["description"]?.Type == JTokenType.String ? extraction["description"]!.Value<string>()?.Trim() : null;
            var currency = extraction?["currency"]?.Type == JTokenType.String ? extraction["currency"]!.Value<string>()?.Trim().ToUpperInvariant() : null;
            if (string.IsNullOrEmpty(currency) || CampaignChannels.SameCurrency(currency, siteCurrency))
                currency = null;

            var remarks = new List<string>();
            if (extraction?["duplicate"] is JObject)
                remarks.Add("Cette facture semble déjà enregistrée.");
            if (extraction?["confidence"]?["overall"]?.Type is JTokenType.Float or JTokenType.Integer
                && extraction["confidence"]!["overall"]!.Value<double>() < 0.7)
                remarks.Add("Lecture incertaine : vérifiez chaque champ.");
            if (extraction?["notes"] is JArray notes)
                remarks.AddRange(notes.Where(n => n.Type == JTokenType.String).Select(n => n.Value<string>()!).Where(n => !string.IsNullOrWhiteSpace(n)));

            var name = string.IsNullOrWhiteSpace(merchant) ? description : merchant;
            return new CampaignDocumentPrefill
            {
                AmountText = amount.HasValue ? CampaignRequestBuilder.FormatAmount(amount.Value, culture) : null,
                Date = ReadDate(extraction?["date"]),
                Name = string.IsNullOrWhiteSpace(name) ? null : name,
                Channel = CampaignChannels.Guess(merchant) ?? CampaignChannels.Guess(description),
                Currency = currency,
                Remarks = remarks,
            };
        }

        /// <summary>Newtonsoft lit « 2026-11-01 » comme une date dans un JObject : les deux formes sont acceptées.</summary>
        private static DateTime? ReadDate(JToken? token) => token?.Type switch
        {
            JTokenType.Date => token.Value<DateTime>().Date,
            JTokenType.String => CampaignDates.Parse(token.Value<string>()),
            _ => null,
        };
    }
}
