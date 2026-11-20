using System.Globalization;
using System.Net;
using Easymakemoney.Models.Api;
using Newtonsoft.Json.Linq;

namespace Easymakemoney.Models
{
    /// <summary>
    /// Commande d'arrivée d'un transfert de stock entre boutiques du groupe (lot backend G-5) : elle ne se modifie pas
    /// et ne se supprime pas (409 TRANSFER_ORDER_LOCKED), mais elle se réceptionne comme les autres.
    /// </summary>
    public static class TransferOrderRules
    {
        public const string Badge = "Transfert de stock";

        public const string Notice = "Cette commande vient d'un transfert entre boutiques du groupe : elle ne se modifie pas et ne se supprime pas. Vous pouvez la réceptionner.";

        /// <summary>
        /// Modifier, supprimer, ajouter ou modifier ses lignes et ses articles, ses frais de port ou son voyage :
        /// ni dans une période clôturée, ni pour la commande d'arrivée d'un transfert. La réception n'en dépend pas.
        /// </summary>
        public static bool CanEdit(bool periodClosed, bool groupTransfer) => !periodClosed && !groupTransfer;

        /// <summary>Paramètre de navigation ajouté seulement pour une commande de transfert (les autres routes ne changent pas).</summary>
        public static string RouteSuffix(bool groupTransfer) => groupTransfer ? "&Transfer=true" : string.Empty;
    }

    /// <summary>Lecture de « GET /api/group » : seule compte la présence d'un groupe (le site en fait partie, siège compris).</summary>
    public static class GroupMembership
    {
        public static bool IsMember(JToken? json) =>
            json is JObject root && root["group"] is { } group && group.Type is not (JTokenType.Null or JTokenType.Undefined);
    }

    /// <summary>
    /// Ligne d'un transfert à envoyer : variante de la boutique source et quantité (sans coût pour la source),
    /// avec product { id, name } et variant { id, size, color } (variant nul pour un article sans variante).
    /// </summary>
    public sealed class GroupTransferLine
    {
        public int Line { get; init; }
        public int? SourceVariantId { get; init; }
        public int? TargetVariantId { get; init; }
        public int Quantity { get; init; }

        public string? ProductName { get; init; }
        public string? VariantSize { get; init; }
        public string? VariantColor { get; init; }

        /// <summary>
        /// « Casquette — M, Bleu » ; sans variante, « Casquette ». Sans product (routes antérieures à l'ajout du backend),
        /// la référence de la variante.
        /// </summary>
        public string Label
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(ProductName))
                {
                    var details = new[] { VariantSize, VariantColor }.Where(part => !string.IsNullOrWhiteSpace(part)).ToList();
                    return details.Count > 0 ? $"{ProductName} — {string.Join(", ", details)}" : ProductName!;
                }
                return SourceVariantId is > 0 ? $"Variante n° {SourceVariantId}" : $"Ligne {Line}";
            }
        }

        public string QuantityText => Quantity == 1 ? "1 article" : $"{Quantity} articles";

        internal static GroupTransferLine Read(JObject item)
        {
            var product = item["product"] as JObject;
            var variant = item["variant"] as JObject;
            return new GroupTransferLine
            {
                Line = InvoiceExtraction.Int(item["line"]) ?? 0,
                SourceVariantId = InvoiceExtraction.Int(item["sourceVariantId"]),
                TargetVariantId = InvoiceExtraction.Int(item["targetVariantId"]),
                Quantity = InvoiceExtraction.Int(item["quantity"]) ?? 0,
                ProductName = InvoiceExtraction.Text(product?["name"]),
                VariantSize = InvoiceExtraction.Text(variant?["size"]),
                VariantColor = InvoiceExtraction.Text(variant?["color"]),
            };
        }
    }

    /// <summary>
    /// Transfert que la boutique source doit envoyer (GET /api/site/group-transfers → { transfers: [...] }) :
    /// états requested, sending et send_failed.
    /// </summary>
    public sealed class GroupTransfer
    {
        public int Id { get; init; }
        public string State { get; init; } = string.Empty;
        /// <summary>Code du site de destination (texte, par exemple « milan »).</summary>
        public string? Target { get; init; }

        /// <summary>Nom de la boutique de destination (« Boutique de Milan »), quand la route le rend.</summary>
        public string? TargetName { get; init; }
        public string? Currency { get; init; }
        public DateTime? RequestedAt { get; init; }
        public List<GroupTransferLine> Lines { get; init; } = new();

        /// <summary>Le nom de la destination, sinon son code.</summary>
        public string TargetText => !string.IsNullOrWhiteSpace(TargetName)
            ? $"Vers {TargetName}"
            : !string.IsNullOrWhiteSpace(Target) ? $"Vers {Target}" : "Vers une boutique du groupe";

        public string StateText => StateLabel(State);

        public string RequestedText => RequestedAt is { } date
            ? $"Demandé le {Utilities.FrenchDate.Format(date, "d MMMM yyyy 'à' H' h 'mm")}"
            : string.Empty;

        public bool HasRequestedText => RequestedText.Length > 0;

        /// <summary>Envoi possible depuis la liste : une demande, ou un envoi en échec (le stock est revenu).</summary>
        public bool IsSendable => State is "requested" or "send_failed";

        public static string StateLabel(string? state) => state switch
        {
            "requested" => "À envoyer",
            "sending" => "Envoi en cours",
            "send_failed" => "Échec de l'envoi",
            "sent" => "Envoyé",
            "received" => "Reçu",
            "cancelled" => "Annulé",
            _ => string.IsNullOrWhiteSpace(state) ? string.Empty : state!,
        };

        public static List<GroupTransfer> ReadList(JToken? json)
        {
            var items = json switch
            {
                JObject root => root["transfers"] as JArray,
                JArray array => array,
                _ => null,
            };
            if (items == null)
                return new List<GroupTransfer>();

            return items.OfType<JObject>()
                .Select(Read)
                .Where(transfer => transfer.Id > 0)
                .ToList();
        }

        public static GroupTransfer Read(JObject item) => new()
        {
            Id = InvoiceExtraction.Int(item["id"]) ?? 0,
            State = InvoiceExtraction.Text(item["state"]) ?? string.Empty,
            Target = InvoiceExtraction.Text(item["target"]),
            TargetName = InvoiceExtraction.Text(item["targetName"]),
            Currency = InvoiceExtraction.Text(item["currency"]),
            RequestedAt = Date(item["requestedAt"]),
            Lines = (item["lines"] as JArray)?.OfType<JObject>().Select(GroupTransferLine.Read).ToList() ?? new List<GroupTransferLine>(),
        };

        /// <summary>Date lue en heure locale (Newtonsoft a pu déjà la transformer en DateTime).</summary>
        internal static DateTime? Date(JToken? token)
        {
            switch (token)
            {
                case JValue { Value: DateTime dateTime }:
                    return dateTime.Kind == DateTimeKind.Utc ? dateTime.ToLocalTime() : dateTime;
                case JValue { Value: DateTimeOffset offset }:
                    return offset.LocalDateTime;
                case JValue { Type: JTokenType.String } text
                    when DateTimeOffset.TryParse(text.Value<string>(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed):
                    return parsed.LocalDateTime;
                default:
                    return null;
            }
        }
    }

    /// <summary>Ce que l'écran propose après un refus de l'envoi.</summary>
    public enum TransferSendAction
    {
        /// <summary>Aucun bouton particulier : le texte du refus (et le tableau du stock manquant).</summary>
        None,
        /// <summary>TRANSFER_STOCK_BUSY : « Réessayer ».</summary>
        Retry,
        /// <summary>TRANSFER_SEND_FAILED : « Renvoyer ».</summary>
        Resend,
        /// <summary>TRANSFER_SEND_PENDING : aucun bouton, relecture bornée de la liste.</summary>
        Wait,
        /// <summary>Déjà en cours d'envoi, déjà envoyé, annulé : la liste est relue.</summary>
        Reload,
    }

    /// <summary>Ligne du refus TRANSFER_STOCK_SHORT : quantité demandée et quantité disponible d'une variante.</summary>
    public sealed class TransferShortLine
    {
        public int? VariantId { get; init; }
        public int Requested { get; init; }
        public int Available { get; init; }
        public string Label { get; init; } = string.Empty;

        public string RequestedText => Requested.ToString(CultureInfo.InvariantCulture);
        public string AvailableText => Available.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>Refus de « POST /api/site/group-transfers/{id}/send », lu d'après le contrat G-5 § 10.2.</summary>
    public sealed class TransferSendRefusal
    {
        public const string PendingNotice = "Vérification en cours, revenez plus tard.";

        public string Code { get; init; } = string.Empty;
        public string Message { get; init; } = string.Empty;
        public TransferSendAction Action { get; init; }
        public List<TransferShortLine> ShortLines { get; init; } = new();

        public bool HasShortLines => ShortLines.Count > 0;

        /// <summary>Texte du bouton proposé, vide s'il n'y en a pas.</summary>
        public string ActionText => Action switch
        {
            TransferSendAction.Retry => "Réessayer",
            TransferSendAction.Resend => "Renvoyer",
            _ => string.Empty,
        };

        public static TransferSendAction ActionFor(string? code) => code switch
        {
            "TRANSFER_STOCK_BUSY" => TransferSendAction.Retry,
            "TRANSFER_SEND_FAILED" => TransferSendAction.Resend,
            "TRANSFER_SEND_PENDING" => TransferSendAction.Wait,
            "TRANSFER_ALREADY_SENDING" or "TRANSFER_ALREADY_SENT" or "TRANSFER_CANCELLED" => TransferSendAction.Reload,
            _ => TransferSendAction.None,
        };

        /// <param name="lines">Lignes du transfert, pour nommer les variantes du tableau du stock manquant.</param>
        public static TransferSendRefusal From(ApiError error, HttpStatusCode? statusCode, IEnumerable<GroupTransferLine>? lines = null)
        {
            var code = error.Code ?? string.Empty;
            var action = ActionFor(code);

            // Textes français des codes du transfert ; les autres refus gardent leur texte.
            var message = IsTransferCode(code)
                ? ApiError.GetFallbackMessage(code, statusCode ?? HttpStatusCode.Conflict, error.RetryAfterSeconds)
                : error.Message;
            if (action == TransferSendAction.Wait)
                message = PendingNotice;

            var shortLines = new List<TransferShortLine>();
            if (code == "TRANSFER_STOCK_SHORT" && error.Body?["lines"] is JArray items)
            {
                var known = lines?.ToList() ?? new List<GroupTransferLine>();
                foreach (var item in items.OfType<JObject>())
                {
                    var variantId = InvoiceExtraction.Int(item["variantId"]);
                    var line = known.FirstOrDefault(l => l.SourceVariantId == variantId && variantId.HasValue);
                    shortLines.Add(new TransferShortLine
                    {
                        VariantId = variantId,
                        Requested = InvoiceExtraction.Int(item["requested"]) ?? 0,
                        Available = InvoiceExtraction.Int(item["available"]) ?? 0,
                        Label = line?.Label ?? (variantId is > 0 ? $"Variante n° {variantId}" : "Article"),
                    });
                }
            }

            return new TransferSendRefusal { Code = code, Message = message, Action = action, ShortLines = shortLines };
        }

        private static bool IsTransferCode(string code) =>
            code.StartsWith("TRANSFER_", StringComparison.Ordinal) || code == "GROUP_SHOP_UNKNOWN";
    }

    /// <summary>
    /// Relecture de la liste après TRANSFER_SEND_PENDING : toutes les 5 s, tant que l'écran est visible et que l'envoi
    /// est en vérification, au plus 2 minutes.
    /// </summary>
    public static class TransferPendingPolling
    {
        public static readonly TimeSpan Interval = TimeSpan.FromSeconds(5);

        public static readonly TimeSpan MaxDuration = TimeSpan.FromMinutes(2);

        /// <summary>Nombre maximal de relectures (2 minutes / 5 s).</summary>
        public static int MaxAttempts => (int)(MaxDuration.Ticks / Interval.Ticks);

        public static bool ShouldPollAgain(int attemptsDone, TimeSpan elapsed, bool visible, bool stillPending) =>
            visible && stillPending && attemptsDone < MaxAttempts && elapsed + Interval <= MaxDuration;

        /// <summary>
        /// L'envoi est-il toujours en vérification ? Oui tant que le transfert reste dans la liste sans être passé en échec :
        /// il en disparaît une fois envoyé ; en échec, l'écran propose « Renvoyer ».
        /// </summary>
        public static bool IsStillPending(GroupTransfer? transfer) =>
            transfer != null && transfer.State != "send_failed";
    }
}
