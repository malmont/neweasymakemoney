using Easymakemoney.Utilities;

namespace Easymakemoney.Models
{
    /// <summary>
    /// Frais fixes (GET gestion/frais-fixes, lot backend FF1, docs/frais-fixes-gestion.md) : les mouvements prévus sortants et récurrents
    /// marqués « frais fixe », avec leur équivalent mensuel calculé par le backend. monthlyTotal et byCategory ne comptent que les actifs.
    /// </summary>
    public class FixedCostList
    {
        public string? currency { get; set; }
        public long monthlyTotal { get; set; }
        public List<FixedCostCategory> byCategory { get; set; } = new();
        public List<FixedCostItem> items { get; set; } = new();

        /// <summary>Actifs d'abord, puis à venir, puis terminés ; les plus lourds en tête.</summary>
        public List<FixedCostItem> SortedItems(DateTime today) => items
            .Select(item => item.WithCurrency(currency))
            .OrderBy(item => item.StateOn(today))
            .ThenByDescending(item => item.monthlyEquivalent)
            .ToList();

        public List<FixedCostCategory> Categories() => byCategory
            .Where(category => category.monthly != 0)
            .OrderByDescending(category => category.monthly)
            .Select(category => category.WithCurrency(currency))
            .ToList();

        public string MonthlyTotalText => SiteCurrency.FormatCents(monthlyTotal, null, currency);
    }

    public class FixedCostCategory
    {
        public string? category { get; set; }
        public string? label { get; set; }
        public long monthly { get; set; }

        [JsonIgnore]
        public string? Currency { get; set; }

        public FixedCostCategory WithCurrency(string? currency)
        {
            Currency = currency;
            return this;
        }

        public string MonthlyText => $"{SiteCurrency.FormatCents(monthly, null, Currency)} par mois";
    }

    public enum FixedCostState
    {
        Active = 0,
        Upcoming = 1,
        Ended = 2,
    }

    public class FixedCostItem
    {
        public int id { get; set; }
        public string? label { get; set; }
        public string? category { get; set; }
        public string? categoryLabel { get; set; }
        public long amount { get; set; }
        public string? recurrence { get; set; }
        public long monthlyEquivalent { get; set; }
        public string? startDate { get; set; }
        public string? endDate { get; set; }
        public bool active { get; set; }
        public bool fixedCostDefault { get; set; }

        [JsonIgnore]
        public string? Currency { get; set; }

        public FixedCostItem WithCurrency(string? currency)
        {
            Currency = currency;
            return this;
        }

        public bool CanDelete => id > 0;

        public string AmountText => recurrence == "monthly"
            ? $"{Money(amount)} par mois"
            : $"{Money(amount)} {TreasuryChoices.RecurrenceLabel(recurrence).ToLowerInvariant()}, soit {Money(monthlyEquivalent)} par mois";

        public FixedCostState StateOn(DateTime today)
        {
            if (active)
                return FixedCostState.Active;
            return TreasuryDates.Parse(startDate) is { } start && start.Date > today.Date ? FixedCostState.Upcoming : FixedCostState.Ended;
        }

        public string StatusText => StatusTextOn(DateTime.Today);

        public string StatusTextOn(DateTime today)
        {
            var parts = new List<string> { categoryLabel ?? category ?? string.Empty };
            switch (StateOn(today))
            {
                case FixedCostState.Upcoming:
                    parts.Add($"commence le {TreasuryDates.Short(startDate)}");
                    break;
                case FixedCostState.Ended:
                    parts.Add(endDate != null ? $"terminé le {TreasuryDates.Short(endDate)}" : "terminé");
                    break;
                default:
                    parts.Add(endDate != null ? $"jusqu'au {TreasuryDates.Short(endDate)}" : $"depuis le {TreasuryDates.Short(startDate)}");
                    break;
            }
            if (!fixedCostDefault)
                parts.Add("marqué à la main");
            return string.Join(" · ", parts.Where(part => part.Length > 0));
        }

        private string Money(long cents) => SiteCurrency.FormatCents(cents, null, Currency);
    }

    /// <summary>Règle par défaut du backend (FF1) : ces catégories sont des frais fixes sauf choix contraire du gérant.</summary>
    public static class FixedCostRule
    {
        private static readonly HashSet<string> DefaultCategories = new() { "rent", "salaries", "insurance", "subscription", "utilities" };

        public static bool IsDefault(string? category) => category != null && DefaultCategories.Contains(category);

        /// <summary>Seule une sortie récurrente peut être un frais fixe ; une entrée ou une dépense ponctuelle jamais.</summary>
        public static bool CanApply(string? direction, string? recurrence) =>
            direction == "out" && recurrence is "weekly" or "monthly" or "quarterly" or "yearly";
    }
}
