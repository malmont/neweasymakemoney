namespace Easymakemoney.Models
{
    /// <summary>Une commande de la collection choisie, avec son frais de port s'il existe.</summary>
    public sealed class FraisDePortCommandeRow
    {
        public required ListCommand Commande { get; init; }

        public FraisDePort? Frais { get; init; }

        /// <summary>Période clôturée : plus d'ajout ni de suppression.</summary>
        public bool PeriodClosed { get; init; }

        public string CommandeName => Commande.Name;

        public bool HasFrais => Frais != null;

        public bool CanAddFrais => Frais == null && !PeriodClosed;

        public bool CanDeleteFrais => Frais != null && !PeriodClosed;

        /// <summary>Lot EDIT : un frais de port d'une période ouverte se modifie.</summary>
        public bool CanEditFrais => Frais != null && !PeriodClosed;

        /// <summary>Prix dans la devise de la commande ; en devise étrangère, le montant converti par le serveur (« ≈ … »).</summary>
        public string PriceText
        {
            get
            {
                if (Frais?.price is not long price)
                    return string.Empty;
                var culture = System.Globalization.CultureInfo.CurrentCulture;
                var currency = Frais.currency ?? Commande.currency;
                var text = Utilities.SiteCurrency.FormatCents(price, culture, currency);
                return Frais.priceConverted is long converted && !string.IsNullOrWhiteSpace(currency)
                       && !string.IsNullOrWhiteSpace(Frais.siteCurrency)
                       && !string.Equals(currency, Frais.siteCurrency, StringComparison.OrdinalIgnoreCase)
                    ? $"{text} (≈ {Utilities.SiteCurrency.FormatCents(converted, culture, Frais.siteCurrency)})"
                    : text;
            }
        }

        public string Image => Frais?.image ?? Commande.Photo;

        public string DetailText
        {
            get
            {
                if (Frais == null)
                    return "Aucun frais de port";

                var transporteur = Frais.transporteur?.name;
                return string.IsNullOrWhiteSpace(transporteur) ? Frais.name : $"{transporteur} · {Frais.name}";
            }
        }
    }
}
