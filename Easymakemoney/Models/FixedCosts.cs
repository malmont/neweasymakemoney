using Easymakemoney.Utilities;

namespace Easymakemoney.Models
{
    /// <summary>
    /// Frais fixes tirés des mouvements prévus de la trésorerie : sorties récurrentes, toujours en cours.
    /// Un remboursement de prêt n'en fait pas partie (c'est du capital, pas une charge).
    /// </summary>
    public static class FixedCosts
    {
        public static IEnumerable<TreasuryMovement> Select(IEnumerable<TreasuryMovement> movements) => movements
            .Where(m => !m.IsIncoming && m.recurrence is "weekly" or "monthly" or "quarterly" or "yearly"
                        && m.category != "loan" && m.nextDate != null)
            .OrderByDescending(MonthlyEquivalent);

        /// <summary>Montant ramené au mois, en cents : hebdomadaire × 52 ÷ 12, trimestriel ÷ 3, annuel ÷ 12.</summary>
        public static long MonthlyEquivalent(TreasuryMovement movement) => movement.recurrence switch
        {
            "weekly" => (long)Math.Round(movement.amount * 52m / 12m),
            "quarterly" => (long)Math.Round(movement.amount / 3m),
            "yearly" => (long)Math.Round(movement.amount / 12m),
            _ => movement.amount,
        };

        public static long MonthlyTotal(IEnumerable<TreasuryMovement> fixedCosts) => fixedCosts.Sum(MonthlyEquivalent);

        public static string MonthlyText(TreasuryMovement movement) =>
            movement.recurrence == "monthly"
                ? $"{SiteCurrency.FormatCents(movement.amount, null, movement.Currency)} par mois"
                : $"{SiteCurrency.FormatCents(movement.amount, null, movement.Currency)} {TreasuryChoices.RecurrenceLabel(movement.recurrence).ToLowerInvariant()}, soit {SiteCurrency.FormatCents(MonthlyEquivalent(movement), null, movement.Currency)} par mois";
    }
}
