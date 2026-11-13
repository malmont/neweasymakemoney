using Newtonsoft.Json.Linq;

namespace Easymakemoney.Services
{
    /// <summary>Lot IA-FACT-1 : le fil de conversation d'une facture fournisseur (contrat : docs/conversation-gestion.md du backend).</summary>
    public interface IInvoiceConversationService
    {
        /// <summary>POST /api/gestion/conversations {documentId} : ouvre le fil, ou rend celui déjà ouvert pour ce document.</summary>
        Task<InvoiceConversation> OpenAsync(int documentId);

        Task<InvoiceConversation> GetAsync(int id);

        /// <summary>POST …/answers : gratuit, aucune IA ; rend le fil à jour.</summary>
        Task<InvoiceConversation> AnswerAsync(int id, string questionId, JToken? value);

        /// <summary>POST …/confirm {previewHash} : rend le résultat de la confirmation, avec thread et undo.</summary>
        Task<JObject?> ConfirmAsync(int id, string previewHash);

        /// <summary>POST …/undo : rend {undone, thread}.</summary>
        Task<JObject?> UndoAsync(int id);
    }

    public class InvoiceConversationService : IInvoiceConversationService
    {
        private const string BaseUrl = "gestion/conversations";
        private readonly IHttpService _httpService;

        public InvoiceConversationService(IHttpService httpService)
        {
            _httpService = httpService;
        }

        public Task<InvoiceConversation> OpenAsync(int documentId) =>
            _httpService.PostAsyncWithAuth<InvoiceConversation>($"{BaseUrl}?locale=fr", new JObject { ["documentId"] = documentId });

        public Task<InvoiceConversation> GetAsync(int id) =>
            _httpService.GetAsync<InvoiceConversation>($"{BaseUrl}/{id}?locale=fr");

        public Task<InvoiceConversation> AnswerAsync(int id, string questionId, JToken? value) =>
            _httpService.PostAsyncWithAuth<InvoiceConversation>($"{BaseUrl}/{id}/answers?locale=fr", new JObject
            {
                ["answers"] = new JArray { new JObject { ["questionId"] = questionId, ["value"] = value ?? JValue.CreateNull() } },
            });

        public Task<JObject?> ConfirmAsync(int id, string previewHash) =>
            _httpService.PostAsyncWithAuth<JObject?>($"{BaseUrl}/{id}/confirm?locale=fr", new JObject { ["previewHash"] = previewHash });

        public Task<JObject?> UndoAsync(int id) =>
            _httpService.PostAsyncWithAuth<JObject?>($"{BaseUrl}/{id}/undo?locale=fr", new JObject());
    }
}
