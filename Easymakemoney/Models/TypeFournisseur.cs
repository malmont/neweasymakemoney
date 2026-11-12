// Code antérieur aux annotations de nullité : avertissements coupés ici, sans effet sur l'exécution.
#nullable disable warnings
namespace Easymakemoney.Models
{
    public class TypeFournisseur
    {
        public int id { get; set; }
        public string name { get; set; }
        public string? photo { get; set; }
    }
}
