using System.Globalization;

namespace Easymakemoney.Utilities
{
    /// <summary>Dates en français du Canada, avec « 1er » pour le premier jour du mois (« 1er décembre 2026 »).</summary>
    public static class FrenchDate
    {
        private static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("fr-CA");

        /// <summary><paramref name="format"/> commence par le jour (« d MMMM yyyy », « d MMMM 'à' H' h 'mm »).</summary>
        public static string Format(DateTime date, string format)
        {
            if (date.Day == 1 && format.StartsWith("d ", StringComparison.Ordinal))
                format = "'1er'" + format[1..];
            return date.ToString(format, Culture);
        }
    }
}
