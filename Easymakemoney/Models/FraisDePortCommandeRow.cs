namespace Easymakemoney.Models
{
    /// <summary>Une commande de la collection choisie, avec son frais de port s'il existe.</summary>
    public sealed class FraisDePortCommandeRow
    {
        public required ListCommand Commande { get; init; }

        public FraisDePort? Frais { get; init; }

        public string CommandeName => Commande.Name;

        public bool HasFrais => Frais != null;

        public bool CanAddFrais => Frais == null;

        public long? Price => Frais?.price;

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
