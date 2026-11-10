using Newtonsoft.Json.Linq;

namespace Easymakemoney.Services
{
    /// <summary>Réception de stock d'une commande fournisseur (lot R1).</summary>
    public interface ICommandeReceptionService
    {
        /// <summary>
        /// « POST commandes/{id}/reception » : sans corps, reçoit tout le reste de la commande ; avec { lines: [{ lineId, variantId }] },
        /// reçoit les lignes dont le gérant a choisi la variante. Idempotent : rien n'est reçu deux fois.
        /// </summary>
        Task<CommandeReception> ReceiveAsync(int commandeId, JObject? body = null);
    }
}
