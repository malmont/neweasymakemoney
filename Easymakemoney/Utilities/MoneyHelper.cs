namespace Easymakemoney.Utilities;

public static class MoneyHelper
{
    /// <summary>
    /// Fonction pure de conversion d'un montant décimal en centimes entiers (long).
    /// Si la valeur est null, retourne 0.
    /// Convertit en decimal avant multiplication pour éviter les imprécisions flottantes IEEE 754.
    /// Applique l'arrondi arithmétique standard MidpointRounding.AwayFromZero.
    /// Exemples : 12.345 -> 1235, 1.005 -> 101, 2.675 -> 268, -12.345 -> -1235.
    /// </summary>
    public static long ToCents(double? valeur)
    {
        if (valeur == null)
            return 0L;

        var decimalValue = (decimal)valeur.Value;
        return (long)Math.Round(decimalValue * 100m, MidpointRounding.AwayFromZero);
    }
}

