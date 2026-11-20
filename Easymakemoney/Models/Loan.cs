using System.Globalization;
using System.Net;
using Easymakemoney.Converters;
using Easymakemoney.Services.Api;
using Easymakemoney.Utilities;
using Newtonsoft.Json.Linq;

namespace Easymakemoney.Models
{
    /// <summary>
    /// Prêts (lot backend FIS-1) : GET gestion/prets → { loans }. Chaque prêt porte sa fiche et son résumé du jour.
    /// Montants en cents, taux en millionièmes (ratePpm : 45000 = 4,5 %), dates AAAA-MM-JJ (jour du magasin).
    /// </summary>
    public class LoanList
    {
        public List<Loan> loans { get; set; } = new();

        /// <summary>
        /// Total restant dû aujourd'hui des prêts dans la devise du site : un prêt dans une autre devise est listé,
        /// mais il reste hors trésorerie et hors résultats, donc hors de ce total.
        /// </summary>
        public long RemainingTotal(string? siteCurrency) => loans
            .Where(loan => loan.IsSiteCurrency(siteCurrency))
            .Sum(loan => loan.remainingToday);

        public bool HasOtherCurrency(string? siteCurrency) => loans.Any(loan => !loan.IsSiteCurrency(siteCurrency));
    }

    /// <summary>Fiche d'un prêt, avec son résumé du jour ; GET gestion/prets/{id} y ajoute l'échéancier complet.</summary>
    public class Loan
    {
        public int id { get; set; }
        public string? label { get; set; }

        /// <summary>Prêteur, facultatif ; jamais envoyé à l'IA.</summary>
        public string? lender { get; set; }

        /// <summary>Montant emprunté, en cents.</summary>
        public long amount { get; set; }
        public string? currency { get; set; }
        public long ratePpm { get; set; }

        /// <summary>Durée en mois, différé compris.</summary>
        public int durationMonths { get; set; }

        /// <summary>1 (mensuelle) ou 3 (trimestrielle).</summary>
        public int frequencyMonths { get; set; } = 1;
        public string? disbursedOn { get; set; }
        public string? firstDueOn { get; set; }

        /// <summary>annuity (échéances constantes) ou constant_capital (capital constant).</summary>
        public string? type { get; set; }
        public int deferralMonths { get; set; }

        /// <summary>partial (intérêts seuls) ou total (rien n'est payé, les intérêts s'ajoutent au capital).</summary>
        public string? deferralKind { get; set; }

        /// <summary>null (aucune), fixed (insuranceAmount par échéance), initial ou remaining (insuranceRatePpm).</summary>
        public string? insuranceMode { get; set; }
        public long? insuranceAmount { get; set; }
        public long? insuranceRatePpm { get; set; }

        /// <summary>Frais de dossier, payés le jour du déblocage.</summary>
        public long fees { get; set; }
        public List<LoanPrepayment> prepayments { get; set; } = new();

        /// <summary>Notes du gérant ; jamais envoyées à l'IA.</summary>
        public string? notes { get; set; }

        // Résumé du jour
        public long remainingToday { get; set; }

        [JsonConverter(typeof(LoanInstalmentOrDateConverter))]
        public LoanInstalment? nextInstalment { get; set; }
        public string? lastDueOn { get; set; }

        /// <summary>Intérêts ajoutés au capital pendant un différé total.</summary>
        public long capitalized { get; set; }
        public LoanTotals totals { get; set; } = new();

        /// <summary>Échéancier complet : seulement sur la fiche (GET gestion/prets/{id}).</summary>
        public List<LoanInstalment> schedule { get; set; } = new();

        /// <summary>Sans devise reçue, le prêt est dans celle du site ; sans devise du site connue, on ne peut pas l'écarter.</summary>
        public bool IsSiteCurrency(string? siteCurrency) =>
            string.IsNullOrWhiteSpace(currency) || string.IsNullOrWhiteSpace(siteCurrency)
            || string.Equals(currency.Trim(), siteCurrency.Trim(), StringComparison.OrdinalIgnoreCase);

        public bool IsOtherCurrency => !IsSiteCurrency(SiteCurrency.CurrentCurrency);

        public string OtherCurrencyText => IsOtherCurrency
            ? $"{currency?.ToUpperInvariant()} · {LoanRules.OtherCurrencyText}"
            : string.Empty;

        public string AmountText => Money(amount);
        public string RemainingText => Money(remainingToday);
        public string RemainingLine => $"Restant dû aujourd'hui : {Money(remainingToday)}";
        public string RateText => LoanRules.PercentText(ratePpm);
        public string CapitalText => Money(totals.capital);
        public string CreditCostText => Money(totals.creditCost);
        public string InterestText => Money(totals.interest);
        public string InsuranceText => Money(totals.insurance);
        public string FeesText => Money(totals.fees);
        public string PaidText => Money(totals.paid);
        public bool HasInsurance => totals.insurance > 0;
        public bool HasFees => totals.fees > 0;
        public bool HasPrepaymentFees => totals.prepaymentFees > 0;
        public string PrepaymentFeesText => $"dont {Money(totals.prepaymentFees)} de frais de remboursement anticipé";
        public bool HasCapitalized => capitalized > 0;
        public string CapitalizedText => $"{Money(capitalized)} d'intérêts ajoutés au capital pendant le différé total.";
        public bool HasLender => !string.IsNullOrWhiteSpace(lender);
        public bool HasNotes => !string.IsNullOrWhiteSpace(notes);

        /// <summary>« 4,5 % · 60 mois · mensuelle · Échéances constantes ».</summary>
        public string TermsText => string.Join(" · ", new[]
        {
            RateText,
            $"{durationMonths} mois",
            LoanRules.FrequencyLabel(frequencyMonths),
            LoanRules.TypeLabel(type),
        }.Where(part => part.Length > 0));

        public string DatesText => $"Débloqué le {TreasuryDates.Short(disbursedOn)} · première échéance le {TreasuryDates.Short(firstDueOn)}"
            + (lastDueOn != null ? $" · dernière le {TreasuryDates.Short(lastDueOn)}" : string.Empty);

        public string DeferralText => deferralMonths > 0 ? $"Différé de {deferralMonths} mois, {LoanRules.DeferralLabel(deferralKind)}" : string.Empty;
        public bool HasDeferral => deferralMonths > 0;

        public string InsuranceModeText => LoanRules.InsuranceText(insuranceMode, insuranceAmount, insuranceRatePpm, currency);

        public bool HasNextInstalment => nextInstalment != null;

        public string NextInstalmentText => nextInstalment is { } next
            ? (next.total > 0 ? $"{TreasuryDates.Short(next.date)} · {Money(next.total)}" : TreasuryDates.Short(next.date))
            : "Aucune : le prêt est remboursé.";

        public string NextInstalmentLine => $"Prochaine échéance : {NextInstalmentText}";

        /// <summary>Remboursements anticipés dans l'ordre des dates : leur rang est l'index de la route de retrait.</summary>
        public List<LoanPrepayment> SortedPrepayments() => prepayments
            .Select((prepayment, position) => (prepayment, position))
            .OrderBy(item => item.prepayment.date, StringComparer.Ordinal)
            .ThenBy(item => item.position)
            .Select((item, index) => item.prepayment.WithIndex(index, currency))
            .ToList();

        public List<LoanInstalment> ScheduleWithCurrency() => schedule.Select(line => line.WithCurrency(currency)).ToList();

        private string Money(long cents) => SiteCurrency.FormatCents(cents, null, currency);
    }

    /// <summary>totals : creditCost = intérêts + assurance + frais, le coût total du crédit ; le capital est à part.</summary>
    public class LoanTotals
    {
        public long capital { get; set; }
        public long interest { get; set; }
        public long insurance { get; set; }
        public long fees { get; set; }
        public long prepaymentFees { get; set; }
        public long creditCost { get; set; }
        public long paid { get; set; }
    }

    /// <summary>Une ligne de l'échéancier : { n, date, periodStart, capital, interest, insurance, total, remaining, deferral, prepayment }.</summary>
    public class LoanInstalment
    {
        public int n { get; set; }
        public string? date { get; set; }
        public string? periodStart { get; set; }
        public long capital { get; set; }
        public long interest { get; set; }
        public long insurance { get; set; }
        public long total { get; set; }
        public long remaining { get; set; }

        /// <summary>Échéance de différé : le type (partial, total), ou true ; null ou false hors différé.</summary>
        public JToken? deferral { get; set; }

        /// <summary>Remboursement anticipé tombé dans cette échéance : un montant, ou { amount, fees } ; null sinon.</summary>
        public JToken? prepayment { get; set; }

        [JsonIgnore]
        public string? Currency { get; set; }

        public LoanInstalment WithCurrency(string? currency)
        {
            Currency = currency;
            return this;
        }

        public bool IsDeferral => deferral switch
        {
            null => false,
            { Type: JTokenType.Null } => false,
            { Type: JTokenType.Boolean } token => token.Value<bool>(),
            { Type: JTokenType.String } token => !string.IsNullOrWhiteSpace(token.Value<string>()),
            _ => true,
        };

        /// <summary>Montant du remboursement anticipé de la ligne, en cents (0 sans remboursement).</summary>
        public long PrepaymentAmount => prepayment switch
        {
            { Type: JTokenType.Integer or JTokenType.Float } token => token.Value<long>(),
            JObject item => item["amount"]?.Type is JTokenType.Integer or JTokenType.Float ? item["amount"]!.Value<long>() : 0,
            _ => 0,
        };

        public bool HasPrepayment => PrepaymentAmount > 0;

        public string TitleText => $"N° {n} · {TreasuryDates.Short(date)}";
        public string TotalText => Money(total);

        public string DetailText
        {
            get
            {
                var parts = new List<string> { $"capital {Money(capital)}", $"intérêts {Money(interest)}" };
                if (insurance > 0)
                    parts.Add($"assurance {Money(insurance)}");
                parts.Add($"reste {Money(remaining)}");
                return string.Join(" · ", parts);
            }
        }

        public string NoteText
        {
            get
            {
                var parts = new List<string>();
                if (IsDeferral)
                {
                    var kind = deferral?.Type == JTokenType.String ? deferral.Value<string>() : null;
                    parts.Add(kind == "total" ? "différé total : intérêts ajoutés au capital" : "différé : intérêts seuls");
                }
                if (HasPrepayment)
                    parts.Add($"remboursement anticipé de {Money(PrepaymentAmount)}");
                return string.Join(" · ", parts);
            }
        }

        public bool HasNote => NoteText.Length > 0;

        private string Money(long cents) => SiteCurrency.FormatCents(cents, null, Currency);
    }

    /// <summary>Remboursement anticipé : { date, amount, fees }. La date de fin ne change pas, les échéances suivantes baissent.</summary>
    public class LoanPrepayment
    {
        public string? date { get; set; }
        public long amount { get; set; }
        public long fees { get; set; }

        /// <summary>Rang dans l'ordre des dates, pour DELETE gestion/prets/{id}/remboursements/{index}.</summary>
        [JsonIgnore]
        public int Index { get; set; }

        [JsonIgnore]
        public string? Currency { get; set; }

        public LoanPrepayment WithIndex(int index, string? currency)
        {
            Index = index;
            Currency = currency;
            return this;
        }

        public string TitleText => $"Le {TreasuryDates.Short(date)} · {SiteCurrency.FormatCents(amount, null, Currency)}";
        public string FeesText => fees > 0 ? $"Frais : {SiteCurrency.FormatCents(fees, null, Currency)}" : "Sans frais";
    }

    /// <summary>Aperçu (POST gestion/prets/apercu) : { loan, schedule, totals }, calculé sans rien écrire.</summary>
    public class LoanPreview
    {
        public Loan loan { get; set; } = new();
        public List<LoanInstalment> schedule { get; set; } = new();
        public LoanTotals totals { get; set; } = new();

        /// <summary>L'échéance courante : la première hors différé (la première tout court s'il n'y en a pas).</summary>
        public LoanInstalment? RegularInstalment() =>
            schedule.FirstOrDefault(line => !line.IsDeferral && line.total > 0) ?? schedule.FirstOrDefault();

        public List<LoanInstalment> FirstLines(int count = 3) =>
            schedule.Take(count).Select(line => line.WithCurrency(loan.currency)).ToList();
    }

    /// <summary>Accepte une échéance { n, date, total… } ou une simple date AAAA-MM-JJ pour nextInstalment.</summary>
    public sealed class LoanInstalmentOrDateConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) => objectType == typeof(LoanInstalment);

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            var token = JToken.Load(reader);
            return token.Type switch
            {
                JTokenType.Object => token.ToObject<LoanInstalment>(),
                JTokenType.String => new LoanInstalment { date = token.Value<string>() },
                _ => null,
            };
        }

        public override bool CanWrite => false;

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer) =>
            throw new NotSupportedException();
    }

    /// <summary>Règles et textes des prêts (FIS-1).</summary>
    public static class LoanRules
    {
        public const string ReadOnlyMessage = "Lecture seule : un directeur de groupe ne peut pas modifier les prêts d'une boutique.";
        public const string OtherCurrencyText = "autre devise : hors trésorerie et résultats";
        public const string CapitalNotChargeText = "Le capital remboursé n'est pas une charge : il sort de la trésorerie, pas du résultat. Seuls les intérêts, l'assurance et les frais comptent dans le résultat.";
        public const int MaxLabel = 120;
        public const int MaxLender = 120;
        public const int MaxNotes = 1000;
        public const int MaxDurationMonths = 600;
        public const long MaxRatePpm = 300000;

        public static IReadOnlyList<TreasuryChoice> Types { get; } = new[]
        {
            new TreasuryChoice("annuity", "Échéances constantes"),
            new TreasuryChoice("constant_capital", "Capital constant"),
        };

        public static IReadOnlyList<TreasuryChoice> Frequencies { get; } = new[]
        {
            new TreasuryChoice("1", "mensuelle"),
            new TreasuryChoice("3", "trimestrielle"),
        };

        public static IReadOnlyList<TreasuryChoice> DeferralKinds { get; } = new[]
        {
            new TreasuryChoice("partial", "partiel (intérêts seuls)"),
            new TreasuryChoice("total", "total"),
        };

        /// <summary>Code « none » côté écran : le backend reçoit insuranceMode null.</summary>
        public static IReadOnlyList<TreasuryChoice> InsuranceModes { get; } = new[]
        {
            new TreasuryChoice("none", "Aucune"),
            new TreasuryChoice("fixed", "Montant fixe par échéance"),
            new TreasuryChoice("initial", "% du capital initial"),
            new TreasuryChoice("remaining", "% du capital restant dû"),
        };

        public static string TypeLabel(string? type) =>
            Types.FirstOrDefault(choice => choice.Code == (type ?? "annuity"))?.Label ?? type ?? string.Empty;

        public static string FrequencyLabel(int frequencyMonths) =>
            Frequencies.FirstOrDefault(choice => choice.Code == frequencyMonths.ToString(CultureInfo.InvariantCulture))?.Label ?? $"tous les {frequencyMonths} mois";

        /// <summary>« par mois » ou « par trimestre ».</summary>
        public static string PerFrequency(int frequencyMonths) => frequencyMonths == 3 ? "par trimestre" : "par mois";

        public static string DeferralLabel(string? kind) =>
            DeferralKinds.FirstOrDefault(choice => choice.Code == kind)?.Label ?? kind ?? string.Empty;

        public static string InsuranceText(string? mode, long? amount, long? ratePpm, string? currency) => mode switch
        {
            "fixed" => $"Assurance : {SiteCurrency.FormatCents(amount ?? 0, null, currency)} par échéance",
            "initial" => $"Assurance : {PercentText(ratePpm ?? 0)} du capital initial",
            "remaining" => $"Assurance : {PercentText(ratePpm ?? 0)} du capital restant dû",
            _ => "Sans assurance",
        };

        /// <summary>45000 → 4,5 (en %).</summary>
        public static decimal PpmToPercent(long ppm) => ppm / 10000m;

        /// <summary>45000 → « 4,5 % », avec les séparateurs de la culture.</summary>
        public static string PercentText(long ppm, CultureInfo? culture = null) =>
            $"{PpmToPercent(ppm).ToString("0.####", culture ?? CultureInfo.CurrentCulture)} %";

        /// <summary>Le nombre seul, pour préremplir un champ : 45000 → « 4,5 ».</summary>
        public static string PercentInput(long ppm, CultureInfo? culture = null) =>
            PpmToPercent(ppm).ToString("0.####", culture ?? CultureInfo.CurrentCulture);

        /// <summary>« 4,5 », « 4.5 », « 4,5 % » → 45000 ; null si illisible ou négatif.</summary>
        public static long? PercentToPpm(string? text)
        {
            if (DecimalInputConverter.Parse(text?.Replace("%", string.Empty)) is not { } percent || percent < 0)
                return null;
            return (long)Math.Round((decimal)percent * 10000m, MidpointRounding.AwayFromZero);
        }

        /// <summary>Texte en français d'un code de refus 422 (fields.&lt;champ&gt; = code).</summary>
        public static string ErrorText(string? code) => code switch
        {
            "LOAN_LABEL_INVALID" => $"Donnez un nom au prêt, de {MaxLabel} caractères au plus.",
            "LOAN_AMOUNT_INVALID" => "Saisissez un montant supérieur à zéro, d'un milliard au plus.",
            "LOAN_RATE_INVALID" => "Le taux doit être compris entre 0 et 30 %.",
            "LOAN_DURATION_INVALID" => $"La durée, différé compris, doit être un multiple du rythme des échéances (3 mois pour un rythme trimestriel), de {MaxDurationMonths} mois au plus.",
            "LOAN_FREQUENCY_INVALID" => "Choisissez des échéances mensuelles ou trimestrielles.",
            "LOAN_DATE_INVALID" => "La première échéance doit venir après le déblocage, au plus deux échéances plus tard.",
            "LOAN_TYPE_INVALID" => "Choisissez « Échéances constantes » ou « Capital constant ».",
            "LOAN_DEFERRAL_INVALID" => "Différé refusé : indiquez sa durée en mois, plus courte que le prêt, et son type (partiel ou total).",
            "LOAN_INSURANCE_INVALID" => "Assurance refusée : un montant par échéance, ou un taux en % du capital initial ou restant dû.",
            "LOAN_FEES_INVALID" => "Les frais de dossier doivent être positifs, de 21 474 836,47 au plus.",
            "LOAN_PREPAYMENT_INVALID" => "Remboursement anticipé refusé : il doit venir après le déblocage et avant la dernière échéance, sans dépasser le capital restant dû à sa date (24 au plus).",
            "LOAN_CURRENCY_INVALID" => "Devise inconnue : utilisez un code à trois lettres (EUR, CAD…).",
            "LOAN_NOT_FOUND" => "Ce prêt n'existe plus.",
            "LOAN_PREPAYMENT_NOT_FOUND" => "Ce remboursement anticipé n'existe plus.",
            _ => "Valeur refusée.",
        };

        /// <summary>Champ du backend → zone de l'écran où afficher son erreur.</summary>
        public static string FieldGroup(string field)
        {
            var root = field.Split('.', '[')[0];
            return root switch
            {
                "label" => "label",
                "lender" => "lender",
                "amount" => "amount",
                "currency" => "currency",
                "ratePpm" => "rate",
                "durationMonths" => "duration",
                "frequencyMonths" => "frequency",
                "disbursedOn" or "firstDueOn" => "dates",
                "type" => "type",
                "deferralMonths" or "deferralKind" => "deferral",
                "insuranceMode" or "insuranceAmount" or "insuranceRatePpm" => "insurance",
                "fees" => "fees",
                "prepayments" or "prepayment" or "date" => "prepayment",
                "notes" => "notes",
                _ => "general",
            };
        }

        /// <summary>
        /// Erreurs d'un refus 422 VALIDATION_FAILED { fields: { champ: code } }, regroupées par zone de l'écran ;
        /// vide si l'exception n'est pas un refus de validation.
        /// </summary>
        public static Dictionary<string, string> FieldErrors(Exception exception)
        {
            var errors = new Dictionary<string, string>();
            if (exception is not ApiException { StatusCode: HttpStatusCode.UnprocessableEntity } api)
                return errors;
            if (api.ApiError.Body?["fields"] is JObject fields)
            {
                foreach (var property in fields.Properties())
                {
                    var group = FieldGroup(property.Name);
                    var code = property.Value.Type == JTokenType.String ? property.Value.Value<string>() : property.Value.ToString();
                    if (!errors.ContainsKey(group))
                        errors[group] = ErrorText(code);
                }
            }
            if (errors.Count == 0)
                errors["general"] = api.ApiError.Message;
            return errors;
        }

        /// <summary>403 MODE_FORBIDDEN : directeur d'un groupe entré dans la boutique, lecture seule.</summary>
        public static bool IsReadOnlyRefusal(Exception exception) =>
            exception is ApiException { StatusCode: HttpStatusCode.Forbidden } api && api.ApiError.Code == "MODE_FORBIDDEN";

        /// <summary>
        /// Garde de l'aperçu en direct : seule la réponse de la dernière demande est montrée,
        /// une réponse ancienne arrivée en retard est ignorée.
        /// </summary>
        public static bool IsLatestPreview(int requestVersion, int latestVersion) => requestVersion == latestVersion;

        /// <summary>
        /// PATCH : seuls les champs changés partent. Un champ absent de la saisie mais présent au départ part à null (effacé).
        /// </summary>
        public static Dictionary<string, object?> ChangedFields(IReadOnlyDictionary<string, object?> initial, IReadOnlyDictionary<string, object?> current)
        {
            var changed = new Dictionary<string, object?>();
            foreach (var (key, value) in current)
            {
                initial.TryGetValue(key, out var before);
                if (!SameValue(before, value))
                    changed[key] = value;
            }
            foreach (var key in initial.Keys.Where(key => !current.ContainsKey(key) && initial[key] != null))
                changed[key] = null;
            return changed;
        }

        private static bool SameValue(object? a, object? b) =>
            JToken.DeepEquals(a == null ? JValue.CreateNull() : JToken.FromObject(a), b == null ? JValue.CreateNull() : JToken.FromObject(b));
    }
}
