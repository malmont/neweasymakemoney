using System.Globalization;
using Easymakemoney.Utilities;

namespace Easymakemoney.Models
{
    /// <summary>Résultat d'une période, calculé par le backend. Montants en centimes, taux en %.</summary>
    public sealed class PeriodResult
    {
        public bool frozen { get; set; }
        public PeriodSales sales { get; set; } = new();
        public PeriodOtherRevenue otherRevenue { get; set; } = new();
        public PeriodCogs cogs { get; set; } = new();
        public PeriodExpenses expenses { get; set; } = new();
        public PeriodTotals result { get; set; } = new();
        public PeriodInvestment investment { get; set; } = new();

        public string NetText => Money(result.net);
        public string GrossText => Money(result.gross);
        public string RevenueText => Money(sales.revenue);
        public string UnknownRevenueText => Minus(sales.unknownCostRevenue);
        public string CogsText => Minus(cogs.amount);
        public string EstimatedCogsText => $"dont {Money(cogs.estimatedAmount)} estimés";
        public bool HasEstimatedCogs => cogs.estimatedItems > 0;
        public string ShippingText => Minus(expenses.shipping);
        public string NotesText => Minus(expenses.notes);
        public string OtherExpensesText => Minus(OtherExpensesAmount);

        /// <summary>Libellé reçu (« Publicité ») quand il n'y a qu'une catégorie d'autres frais ; sinon « Autres frais ».</summary>
        public string OtherExpensesTitle
        {
            get
            {
                var labels = expenses.other?
                    .Select(item => string.IsNullOrWhiteSpace(item.label) ? null : item.label.Trim())
                    .Distinct()
                    .ToList();
                return labels is { Count: 1 } && labels[0] != null ? labels[0]! : "Autres frais";
            }
        }

        /// <summary>Achats de marchandises seulement : les frais de port sont déjà comptés dans les frais du résultat.</summary>
        public string InvestmentText => Money(investment.purchases);
        public string OtherRevenueText => Money(otherRevenue.total);

        public bool IsLoss => result.net < 0;
        public bool HasUnknownCost => cogs.unknownCostItems > 0;
        public bool HasUnknownRevenue => sales.unknownCostRevenue > 0;
        public bool HasOtherExpenses => OtherExpensesAmount > 0;
        public bool HasOtherRevenue => otherRevenue.total > 0;
        public bool HasNetRate => result.netRate.HasValue;

        public string NetRateText => result.netRate.HasValue
            ? $"{result.netRate.Value.ToString("0.#", CultureInfo.CurrentCulture)} % des ventes"
            : string.Empty;

        public string UnknownCostTitle => cogs.unknownCostItems > 1
            ? $"{cogs.unknownCostItems} articles sans coût"
            : "1 article sans coût";

        public string UnknownCostDetail => $"{Money(sales.unknownCostRevenue)} de ventes hors marge. Associez-les à une commande pour les compter.";

        private long OtherExpensesAmount => expenses.other?.Sum(item => item.amount) ?? 0;

        private static string Money(long cents) => SiteCurrency.FormatCents(cents, CultureInfo.CurrentCulture);

        private static string Minus(long cents) => $"− {Money(cents)}";
    }

    public sealed class PeriodSales
    {
        public long revenue { get; set; }
        public long unknownCostRevenue { get; set; }
    }

    public sealed class PeriodOtherRevenue
    {
        public long total { get; set; }
    }

    public sealed class PeriodCogs
    {
        public long amount { get; set; }
        public int unknownCostItems { get; set; }
        public int estimatedItems { get; set; }
        public long estimatedAmount { get; set; }
    }

    public sealed class PeriodExpenses
    {
        public long shipping { get; set; }
        public long notes { get; set; }
        public List<PeriodExpenseItem>? other { get; set; }
    }

    public sealed class PeriodExpenseItem
    {
        /// <summary>Catégorie, par exemple « advertising ».</summary>
        public string? kind { get; set; }

        /// <summary>Libellé à afficher, par exemple « Publicité ».</summary>
        public string? label { get; set; }

        public long amount { get; set; }
    }

    public sealed class PeriodTotals
    {
        public long gross { get; set; }
        public long net { get; set; }
        public double? netRate { get; set; }
    }

    public sealed class PeriodInvestment
    {
        public long purchases { get; set; }
        public long shipping { get; set; }
        public long total { get; set; }
    }
}
