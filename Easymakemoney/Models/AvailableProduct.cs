using System.Globalization;
using Easymakemoney.Utilities;

namespace Easymakemoney.Models
{
    public sealed class AvailableProductsPage
    {
        public List<AvailableProduct> items { get; set; } = new();
        public int total { get; set; }
        public int page { get; set; }
        public int limit { get; set; }
    }

    /// <summary>Un article existant, proposé au rattachement à une commande. Montants en centimes.</summary>
    public partial class AvailableProduct : ObservableObject
    {
        public int id { get; set; }
        public string name { get; set; } = string.Empty;
        public string? code { get; set; }
        public string? image { get; set; }
        public double? price { get; set; }
        public double? purchasePrice { get; set; }
        public int? quantity { get; set; }

        [ObservableProperty]
        private bool isSelected;

        /// <summary>Coût saisi pour cette commande ; sans saisie, le backend garde le coût de l'article.</summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CostText))]
        private long? editedCostCents;

        /// <summary>Quantité achetée dans cette commande ; sans saisie, le backend prend le stock actuel.</summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(QuantityText))]
        [NotifyPropertyChangedFor(nameof(HasQuantity))]
        private int? editedQuantity;

        public bool HasQuantity => EditedQuantity.HasValue;

        public string QuantityText => $"Quantité achetée : {EditedQuantity}";

        public string CostText
        {
            get
            {
                var cost = (double?)EditedCostCents ?? purchasePrice;
                return cost.HasValue
                    ? $"Coût : {SiteCurrency.FormatCents(cost.Value, CultureInfo.CurrentCulture)}"
                    : "Coût : non renseigné";
            }
        }

        public string PriceText => price.HasValue
            ? $"Prix : {SiteCurrency.FormatCents(price.Value, CultureInfo.CurrentCulture)}"
            : string.Empty;
    }
}
