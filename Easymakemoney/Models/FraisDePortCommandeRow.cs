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

        public string PriceText => Frais?.price is long price
            ? Utilities.SiteCurrency.FormatCents(price, System.Globalization.CultureInfo.CurrentCulture, Commande.currency)
            : string.Empty;

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
