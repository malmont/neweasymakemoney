using Easymakemoney.Utilities;

namespace Easymakemoney.Models
{
    /// <summary>Voyage d'achat (gestion/voyages, lot backend B1a, docs/budgets-gestion.md § 2).</summary>
    public class Trip
    {
        public int id { get; set; }
        public string? name { get; set; }
        public string? place { get; set; }
        public string? startDate { get; set; }
        public string? endDate { get; set; }
        public int days { get; set; }
        public long budgetTotal { get; set; }
        public long budgetDaily { get; set; }
        public bool budgetDailyEntered { get; set; }
        public string? budgetScope { get; set; }
        public string? currency { get; set; }
        public double? exchangeRate { get; set; }
        public string? siteCurrency { get; set; }
        public long? budgetTotalSite { get; set; }
        public long? budgetDailySite { get; set; }
        public string? notes { get; set; }

        public bool IsForeign => exchangeRate is > 0 && !string.IsNullOrWhiteSpace(currency)
                                 && !string.Equals(currency, siteCurrency, StringComparison.OrdinalIgnoreCase);

        public string Title => string.IsNullOrWhiteSpace(place) ? name ?? "Voyage" : $"{name} · {place}";
        public string DatesText => $"Du {TreasuryDates.Short(startDate)} au {TreasuryDates.Short(endDate)} · {days} jours";
        public string BudgetText => $"Budget {SiteCurrency.FormatCents(budgetTotal, null, currency)} · {SiteCurrency.FormatCents(budgetDaily, null, currency)} par jour";

        public bool IsCurrent(DateTime today) =>
            TreasuryDates.Parse(startDate) is { } start && TreasuryDates.Parse(endDate) is { } end && today.Date >= start && today.Date <= end;
    }

    /// <summary>Suivi d'un voyage, dans la devise du site (GET gestion/voyages/{id}/suivi).</summary>
    public class TripFollowUp
    {
        public string? currency { get; set; }
        public TripFollowUpTrip? trip { get; set; }
        public string? budgetScope { get; set; }
        public bool inProgress { get; set; }
        public int elapsedDays { get; set; }
        public List<TripDay> days { get; set; } = new();
        public TripTotals totals { get; set; } = new();
        public TripToday? today { get; set; }
        public long averageDaily { get; set; }
        public long projectedTotal { get; set; }
        public string? status { get; set; }
        public long overBy { get; set; }

        public string StatusText => status switch
        {
            "over" => overBy > 0 ? $"Au-dessus du budget de {Money(overBy)}" : "Au-dessus du budget",
            "warning" => "Budget bientôt atteint",
            _ => "Dans le budget",
        };

        public Microsoft.Maui.Graphics.Color StatusColor => Microsoft.Maui.Graphics.Color.FromArgb(status switch
        {
            "over" => "#B91C1C",
            "warning" => "#B45309",
            _ => "#047857",
        });

        public string ScopeText => budgetScope == "all"
            ? "Le budget couvre les achats de marchandises et les frais."
            : "Le budget couvre les frais du voyage (déplacement, hôtel, repas). Les achats de marchandises sont montrés à part.";

        public double Progress => totals.budget <= 0 ? 0 : Math.Clamp((double)totals.spent / totals.budget, 0, 1);

        public string SpentText => $"Dépensé {Money(totals.spent)} sur {Money(totals.budget)}";
        public string RemainingText => totals.remaining >= 0 ? $"Reste {Money(totals.remaining)}" : $"Dépassé de {Money(-totals.remaining)}";
        public string AllowanceText => inProgress ? $"Budget prévu à ce jour : {Money(totals.allowanceToDate)}" : string.Empty;
        public string TodayText => today == null ? string.Empty : $"Aujourd'hui : {Money(today.spent)} dépensés, {Money(today.remaining)} restants";
        public bool HasToday => today != null;
        public string AverageText => $"{Money(averageDaily)} par jour en moyenne · {Money(projectedTotal)} prévus à ce rythme";
        public string PurchasesText => $"Achats de marchandises rattachés : {Money(totals.purchases)}";

        public List<TripDay> SpendingDays() => days.Where(day => day.spent != 0 || day.purchases != 0).Select(day => day.WithCurrency(currency)).ToList();

        public string Money(long cents) => SiteCurrency.FormatCents(cents, null, currency);
    }

    public class TripFollowUpTrip
    {
        public int id { get; set; }
        public string? name { get; set; }
        public string? place { get; set; }
        public string? startDate { get; set; }
        public string? endDate { get; set; }
    }

    public class TripDay
    {
        public string? date { get; set; }
        public long purchases { get; set; }
        public long expenses { get; set; }
        public long spent { get; set; }
        public long budget { get; set; }

        [JsonIgnore]
        public string? Currency { get; set; }

        public TripDay WithCurrency(string? currency)
        {
            Currency = currency;
            return this;
        }

        public string DateText => TreasuryDates.Short(date);
        public string SpentText => SiteCurrency.FormatCents(spent, null, Currency);
        public string DetailText => purchases != 0
            ? $"Frais {SiteCurrency.FormatCents(expenses, null, Currency)} · achats {SiteCurrency.FormatCents(purchases, null, Currency)}"
            : $"Budget du jour {SiteCurrency.FormatCents(budget, null, Currency)}";
        public bool IsOver => budget > 0 && spent > budget;
    }

    public class TripTotals
    {
        public long purchases { get; set; }
        public long expenses { get; set; }
        public long spent { get; set; }
        public long budget { get; set; }
        public long remaining { get; set; }
        public long allowanceToDate { get; set; }
    }

    public class TripToday
    {
        public string? date { get; set; }
        public long spent { get; set; }
        public long remaining { get; set; }
    }

    /// <summary>Corps d'un voyage créé ou modifié ; la devise du site est la valeur par défaut.</summary>
    public sealed class TripRequest
    {
        public string name { get; init; } = string.Empty;

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? place { get; init; }

        public string startDate { get; init; } = string.Empty;
        public string endDate { get; init; } = string.Empty;
        public long budgetTotal { get; init; }

        /// <summary>Nul : budget total ÷ nombre de jours (calculé par le backend).</summary>
        public long? budgetDaily { get; init; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? currency { get; init; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public double? exchangeRate { get; init; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? notes { get; init; }
    }
}
