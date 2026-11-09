using System.Globalization;
using Easymakemoney.Utilities;

namespace Easymakemoney.Models
{
    public sealed class RepriseSimulation
    {
        public List<ReprisePeriod> periods { get; set; } = new();
        public RepriseSales sales { get; set; } = new();
        public List<RepriseProduct> products { get; set; } = new();
        public int productsTotal { get; set; }
        public RepriseInvestment investment { get; set; } = new();

        public string SalesText => SiteCurrency.FormatCents(sales.revenue, CultureInfo.CurrentCulture);
        public string KnownInvestmentText => SiteCurrency.FormatCents(investment.known, CultureInfo.CurrentCulture);
        public string EstimatedInvestmentText => SiteCurrency.FormatCents(investment.estimated, CultureInfo.CurrentCulture);
        public string PeriodsText => periods.Count > 1 ? $"{periods.Count} périodes" : "1 période";
        public string ItemsText => $"{sales.items} articles vendus";
        public string UnknownText => sales.unknownCostItems > 1
            ? $"{sales.unknownCostItems} articles vendus sans coût"
            : $"{sales.unknownCostItems} article vendu sans coût";
    }

    public sealed class ReprisePeriod
    {
        public string? name { get; set; }
    }

    public sealed class RepriseSales
    {
        public long revenue { get; set; }
        public int items { get; set; }
        public int unknownCostItems { get; set; }
    }

    public sealed class RepriseInvestment
    {
        public long known { get; set; }
        public long estimated { get; set; }
    }

    /// <summary>Un article vendu pendant le créneau de reprise. Montants en centimes.</summary>
    public partial class RepriseProduct : ObservableObject
    {
        public int productId { get; set; }
        public string name { get; set; } = string.Empty;
        public int quantitySold { get; set; }
        public long revenue { get; set; }
        public string? costSource { get; set; }
        public long? unitCost { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CostText))]
        private long? editedCostCents;

        /// <summary>Le coût d'une commande fournisseur existante ne se remplace pas.</summary>
        public bool CanEditCost => costSource != "commande";

        public string SoldText =>
            $"{quantitySold} vendus · {SiteCurrency.FormatCents(revenue, CultureInfo.CurrentCulture)}";

        public string CostText
        {
            get
            {
                if (EditedCostCents.HasValue)
                    return $"Coût saisi : {Money(EditedCostCents.Value)}";
                if (!unitCost.HasValue)
                    return "Coût inconnu";

                return costSource == "estimated"
                    ? $"Coût estimé : {Money(unitCost.Value)}"
                    : $"Coût : {Money(unitCost.Value)}";
            }
        }

        private static string Money(long cents) => SiteCurrency.FormatCents(cents, CultureInfo.CurrentCulture);
    }

    public sealed class RepriseState
    {
        public bool active { get; set; }
        public string? appliedAt { get; set; }
        public int? months { get; set; }
        public List<RepriseCommande>? commandes { get; set; }
        public List<RepriseSkipped>? skipped { get; set; }

        public string SummaryText
        {
            get
            {
                var count = commandes?.Count ?? 0;
                return $"{months} mois repris · {count} commande{(count > 1 ? "s" : string.Empty)} de reprise";
            }
        }
    }

    public sealed class RepriseCommande
    {
        public string? periodName { get; set; }
        public int productCount { get; set; }
        public int estimatedCount { get; set; }
        public long investment { get; set; }

        public string InvestmentText => SiteCurrency.FormatCents(investment, CultureInfo.CurrentCulture);

        public string DetailText => estimatedCount > 0
            ? $"{productCount} articles, dont {estimatedCount} au coût estimé"
            : $"{productCount} articles";
    }

    public sealed class RepriseSkipped
    {
        public int productId { get; set; }
    }
}
