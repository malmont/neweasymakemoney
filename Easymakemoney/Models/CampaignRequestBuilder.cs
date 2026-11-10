using System.Globalization;
using Easymakemoney.Converters;
using Easymakemoney.Utilities;
using Newtonsoft.Json.Linq;

namespace Easymakemoney.Models
{
    /// <summary>Ce que le gérant a saisi dans le formulaire de campagne (montant et taux en texte).</summary>
    public sealed class CampaignFormInput
    {
        public string? Name { get; set; }
        public string? Channel { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string? AmountText { get; set; }

        /// <summary>Devise de la campagne ; null : devise du site.</summary>
        public string? Currency { get; set; }

        /// <summary>Devise du site (SiteCurrency.CurrentCurrency, ou celle rendue par la campagne).</summary>
        public string? SiteCurrency { get; set; }

        public string? ExchangeRateText { get; set; }
        public IReadOnlyCollection<int> ProductIds { get; set; } = Array.Empty<int>();
        public string? Notes { get; set; }
    }

    public sealed class CampaignRequestResult
    {
        public JObject? Body { get; init; }
        public List<string> Errors { get; init; } = new();
        public bool IsValid => Body != null && Errors.Count == 0;
    }

    /// <summary>
    /// Corps de « POST / PATCH gestion/campagnes » et de « POST gestion/documents/{id}/confirmer » (as: "campaign").
    /// Sans MAUI : testé dans Easymakemoney.Tests.
    /// </summary>
    public static class CampaignRequestBuilder
    {
        /// <summary>Une devise choisie, autre que celle du site (ou celle du site inconnue), demande un taux.</summary>
        public static bool NeedsExchangeRate(string? currency, string? siteCurrency) =>
            !string.IsNullOrWhiteSpace(currency)
            && (string.IsNullOrWhiteSpace(siteCurrency)
                || !string.Equals(currency.Trim(), siteCurrency.Trim(), StringComparison.OrdinalIgnoreCase));

        public static string ExchangeRateLabel(string? currency, string? siteCurrency) =>
            $"Taux : 1 {currency?.Trim().ToUpperInvariant()} en {(string.IsNullOrWhiteSpace(siteCurrency) ? "devise du site" : siteCurrency.Trim().ToUpperInvariant())}";

        /// <summary>Création (POST) ou modification (PATCH, <paramref name="isUpdate"/>) d'une campagne.</summary>
        public static CampaignRequestResult Build(CampaignFormInput input, bool isUpdate = false)
        {
            var errors = new List<string>();
            var body = new JObject();

            var name = input.Name?.Trim();
            if (string.IsNullOrEmpty(name))
                errors.Add("Donnez un nom à la campagne.");
            else
                body["name"] = name;

            if (!CampaignChannels.IsKnown(input.Channel))
                errors.Add("Choisissez le canal de la campagne.");
            else
                body["channel"] = input.Channel;

            if (input.EndDate.Date < input.StartDate.Date)
                errors.Add("La date de fin est avant la date de début.");
            body["startDate"] = CampaignDates.ToApi(input.StartDate);
            body["endDate"] = CampaignDates.ToApi(input.EndDate);

            var amount = DecimalInputConverter.Parse(input.AmountText);
            if (amount is null or < 0)
                errors.Add("Montant invalide.");
            else
                body["amount"] = MoneyHelper.ToCents(amount);

            var currency = string.IsNullOrWhiteSpace(input.Currency) ? null : input.Currency.Trim().ToUpperInvariant();
            if (currency == null)
            {
                // Devise du site : envoyée quand elle est connue, pour qu'une modification quitte une devise étrangère.
                if (!string.IsNullOrWhiteSpace(input.SiteCurrency))
                    body["currency"] = input.SiteCurrency.Trim().ToUpperInvariant();
            }
            else
            {
                body["currency"] = currency;
                if (NeedsExchangeRate(currency, input.SiteCurrency))
                {
                    var rate = DecimalInputConverter.Parse(input.ExchangeRateText);
                    if (rate is null or <= 0)
                        errors.Add("Indiquez le taux de change (un nombre plus grand que zéro).");
                    else
                        body["exchangeRate"] = rate.Value;
                }
            }

            // Sans ciblage, la campagne compte pour tout le magasin ; une liste vide efface le ciblage d'une campagne modifiée.
            var productIds = input.ProductIds.Where(id => id > 0).Distinct().ToList();
            if (productIds.Count > 0 || isUpdate)
                body["productIds"] = new JArray(productIds);

            var notes = input.Notes?.Trim();
            if (!string.IsNullOrEmpty(notes))
                body["notes"] = notes;
            else if (isUpdate)
                body["notes"] = JValue.CreateNull();

            return errors.Count > 0
                ? new CampaignRequestResult { Errors = errors }
                : new CampaignRequestResult { Body = body };
        }

        /// <summary>Confirmation d'une facture de régie lue (document de type note) en campagne.</summary>
        public static CampaignRequestResult BuildConfirmation(CampaignFormInput input)
        {
            var result = Build(input);
            if (!result.IsValid)
                return result;

            var body = new JObject { ["as"] = "campaign" };
            foreach (var property in result.Body!.Properties())
                body[property.Name] = property.Value;
            return new CampaignRequestResult { Body = body };
        }

        /// <summary>Message lisible pour un refus connu du backend ; null : message du serveur.</summary>
        public static string? MessageFor(string? code) => code switch
        {
            "PERIOD_CLOSED" => "La période de la date de début est clôturée. Choisissez une autre date, ou rouvrez la période depuis le tableau de bord.",
            "OUT_OF_CALENDAR" => "Aucune période ne couvre la date de début. Créez d'abord la période correspondante.",
            "EXCHANGE_RATE_REQUIRED" => "Indiquez le taux de change : la devise de la campagne n'est pas celle du site.",
            "CAMPAIGN_NOT_FOUND" => "Cette campagne n'existe plus.",
            "DOCUMENT_CONFIRMED" => "Cette facture est déjà enregistrée.",
            "DOCUMENT_EXPIRED" => "Cette facture n'a pas été validée à temps : elle a été effacée. Lisez-la à nouveau.",
            "DOCUMENT_IN_PROGRESS" => "La lecture de la facture n'est pas terminée. Réessayez dans un instant.",
            _ => null,
        };

        /// <summary>Message d'un refus de suppression.</summary>
        public static string? DeleteMessageFor(string? code) => code switch
        {
            "PERIOD_CLOSED" => "Cette campagne est dans une période clôturée : elle ne peut plus être supprimée. Rouvrez la période depuis le tableau de bord si besoin.",
            "CAMPAIGN_NOT_FOUND" => "Cette campagne a déjà été supprimée.",
            _ => null,
        };

        /// <summary>Taux pour un champ de saisie : 1.3542 → « 1,3542 » en français.</summary>
        public static string FormatRate(double rate, CultureInfo? culture = null) =>
            rate.ToString("0.######", culture ?? CultureInfo.CurrentCulture);

        /// <summary>1250 → « 12,50 » (selon la culture), pour un champ de saisie.</summary>
        public static string FormatAmount(long cents, CultureInfo? culture = null) =>
            (cents / 100m).ToString("0.00", culture ?? CultureInfo.CurrentCulture);
    }
}
