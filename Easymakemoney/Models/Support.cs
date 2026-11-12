using System.Globalization;
using Easymakemoney.Utilities;
using Newtonsoft.Json.Linq;

namespace Easymakemoney.Models
{
    /// <summary>
    /// Module Assistance, lot 1 du backend (docs/assistance.md) : demandes d'assistance traitées par l'équipe du site.
    /// L'appli affiche les valeurs du serveur ; elle ne calcule ni délai ni montant.
    /// </summary>
    public class SupportTicketList
    {
        public List<SupportTicket> items { get; set; } = new();
        public int total { get; set; }
        public int page { get; set; } = 1;
        public int pageSize { get; set; } = 20;

        public bool HasMore => page * pageSize < total;
    }

    public class SupportTicket
    {
        public int id { get; set; }
        public string reference { get; set; } = string.Empty;
        public string subject { get; set; } = string.Empty;
        public string status { get; set; } = "new";
        public string priority { get; set; } = "normal";
        public string channel { get; set; } = "agent";
        public SupportCustomer? customer { get; set; }
        public SupportOrderBrief? order { get; set; }
        public SupportAgent? assignedTo { get; set; }
        public string? dueAt { get; set; }
        public bool overdue { get; set; }
        public bool awaitingReply { get; set; }
        public string? firstResponseAt { get; set; }
        public string? lastMessageAt { get; set; }
        public string? closedAt { get; set; }
        public bool anonymized { get; set; }
        public string? createdAt { get; set; }

        public string StatusText => SupportTexts.Status(status);
        public string PriorityText => SupportTexts.Priority(priority);
        public string ChannelText => SupportTexts.Channel(channel);
        public bool IsClosed => status == "closed";
        public bool IsUrgent => priority is "urgent" or "high";

        public string CustomerText => customer is null
            ? "Client inconnu"
            : string.Join(" · ", new[] { customer.name, customer.email }.Where(part => !string.IsNullOrWhiteSpace(part)));

        public string AssignedText => assignedTo?.name is { Length: > 0 } name ? $"Suivie par {name}" : "Non attribuée";

        /// <summary>Délai de réponse du serveur (jours ouvrés du site) ; « en retard » vient de <c>overdue</c>.</summary>
        public string DueText
        {
            get
            {
                if (IsClosed)
                    return SupportTexts.Date(closedAt) is { Length: > 0 } closed ? $"Fermée le {closed}" : "Fermée";
                if (overdue)
                    return "Réponse en retard";
                return SupportTexts.DateTime(dueAt) is { Length: > 0 } due ? $"Réponse attendue avant le {due}" : string.Empty;
            }
        }

        public bool HasDue => !string.IsNullOrEmpty(DueText);
        public string HeaderText => $"{reference} · {StatusText} · {PriorityText}";
    }

    public class SupportCustomer
    {
        public int? userId { get; set; }
        public string? name { get; set; }
        public string? email { get; set; }
        public string? locale { get; set; }
    }

    public class SupportOrderBrief
    {
        public int id { get; set; }
        public string? reference { get; set; }
    }

    public class SupportAgent
    {
        /// <summary>Nul pour l'auteur d'un message du client.</summary>
        public int? id { get; set; }
        public string? name { get; set; }
    }

    /// <summary>Fiche d'une demande : la ligne de liste, plus les messages et le résumé de commande en lecture.</summary>
    public class SupportTicketDetail : SupportTicket
    {
        public List<SupportMessage> messages { get; set; } = new();
        public SupportOrderSummary? orderSummary { get; set; }

        public bool HasOrder => orderSummary != null;
    }

    public class SupportMessage
    {
        public int id { get; set; }
        public string authorType { get; set; } = "agent";
        public SupportAgent? author { get; set; }
        public string body { get; set; } = string.Empty;
        public bool @internal { get; set; }
        public string? emailStatus { get; set; }
        public string? createdAt { get; set; }
        public List<SupportAttachment> attachments { get; set; } = new();

        public bool IsCustomer => authorType == "customer";
        public bool IsSystem => authorType == "system";
        public bool IsInternal => @internal;
        public bool HasAttachments => attachments.Count > 0;

        public string AuthorText => authorType switch
        {
            "customer" => "Client",
            "system" => "Système",
            _ => author?.name is { Length: > 0 } name ? name : "Équipe",
        };

        /// <summary>« Note interne », ou l'état du courriel envoyé au client pour une réponse de l'équipe.</summary>
        public string KindText
        {
            get
            {
                if (IsInternal)
                    return "Note interne (jamais envoyée)";
                if (authorType != "agent")
                    return string.Empty;
                return emailStatus switch
                {
                    "sent" => "Envoyé au client",
                    "failed" => "Courriel non envoyé",
                    "logged" => "Consigné (envoi des courriels désactivé)",
                    _ => string.Empty,
                };
            }
        }

        public bool HasKind => !string.IsNullOrEmpty(KindText);
        public bool IsEmailFailed => emailStatus == "failed";
        public string WhenText => SupportTexts.DateTime(createdAt);
        public string AttachmentsText => string.Join(", ", attachments.Select(attachment => attachment.name));
    }

    public class SupportAttachment
    {
        public int id { get; set; }
        public string name { get; set; } = string.Empty;
        public string? mime { get; set; }
        public long size { get; set; }
    }

    /// <summary>Résumé en lecture de la commande ou vente liée (liste blanche du serveur : ni coût ni marge).</summary>
    public class SupportOrderSummary
    {
        public int id { get; set; }
        public string? reference { get; set; }
        public string? date { get; set; }
        public string? source { get; set; }
        public string? status { get; set; }
        public List<SupportOrderLine> lines { get; set; } = new();
        public JObject? totals { get; set; }
        public List<SupportOrderPayment> payments { get; set; } = new();
        public List<SupportOrderReturn> returns { get; set; } = new();
        public SupportOrderShipping? shipping { get; set; }

        public string TitleText => string.Join(" · ", new[] { reference, SupportTexts.Date(date), source, status }.Where(part => !string.IsNullOrWhiteSpace(part)));
        public string TotalText => Cents(totals?["total"]) is { } total ? $"Total : {SiteCurrency.FormatCents(total)}" : string.Empty;
        public string LinesText => string.Join("\n", lines.Select(line => line.Text));
        public string PaymentsText => string.Join("\n", payments.Select(payment => payment.Text));
        public string ReturnsText => string.Join("\n", returns.Select(item => item.Text));
        public bool HasPayments => payments.Count > 0;
        public bool HasReturns => returns.Count > 0;

        public string ShippingText => shipping is null
            ? string.Empty
            : string.Join(" · ", new[] { shipping.carrier, shipping.trackingNumber, SupportTexts.Date(shipping.shippedAt) is { Length: > 0 } shipped ? $"expédiée le {shipped}" : null }
                .Where(part => !string.IsNullOrWhiteSpace(part)));

        public bool HasShipping => !string.IsNullOrEmpty(ShippingText);

        internal static long? Cents(JToken? token) =>
            token != null && long.TryParse(token.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : null;
    }

    public class SupportOrderLine
    {
        public string? name { get; set; }
        public string? variant { get; set; }
        public int quantity { get; set; }
        public long? unitPrice { get; set; }
        public long? discount { get; set; }
        public long? total { get; set; }

        public string Text
        {
            get
            {
                var label = string.IsNullOrWhiteSpace(variant) ? name : $"{name} ({variant})";
                return total is { } amount ? $"{quantity} × {label} : {SiteCurrency.FormatCents(amount)}" : $"{quantity} × {label}";
            }
        }
    }

    public class SupportOrderPayment
    {
        public string? method { get; set; }
        public long? amount { get; set; }
        public string? date { get; set; }

        public string Text => $"{method ?? "Paiement"} : {(amount is { } value ? SiteCurrency.FormatCents(value) : "?")}"
            + (SupportTexts.Date(date) is { Length: > 0 } day ? $" ({day})" : string.Empty);
    }

    public class SupportOrderReturn
    {
        public long amount { get; set; }
        public string? date { get; set; }

        public string Text => $"Retour : {SiteCurrency.FormatCents(amount)}" + (SupportTexts.Date(date) is { Length: > 0 } day ? $" ({day})" : string.Empty);
    }

    public class SupportOrderShipping
    {
        public string? carrier { get; set; }
        public string? trackingNumber { get; set; }
        public string? trackingUrl { get; set; }
        public string? shippedAt { get; set; }
    }

    /// <summary>Corps de création d'une demande par l'équipe (appel, comptoir, courriel recopié).</summary>
    public class SupportTicketCreateRequest
    {
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public string Channel { get; set; } = "phone";
        public string Priority { get; set; } = "normal";
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public int? OrderId { get; set; }

        public Dictionary<string, object> ToBody()
        {
            var body = new Dictionary<string, object>
            {
                ["subject"] = Subject.Trim(),
                ["body"] = Body.Trim(),
                ["channel"] = Channel,
                ["priority"] = Priority,
                ["customer"] = new Dictionary<string, object> { ["name"] = CustomerName.Trim(), ["email"] = CustomerEmail.Trim() },
            };
            if (OrderId is > 0)
                body["orderId"] = OrderId.Value;
            return body;
        }
    }

    public static class SupportTexts
    {
        public static readonly string[] Statuses = { "new", "open", "waiting_customer", "closed" };
        public static readonly string[] Priorities = { "low", "normal", "high", "urgent" };

        public static string Status(string? status) => status switch
        {
            "new" => "Nouvelle",
            "open" => "En cours",
            "waiting_customer" => "Attend le client",
            "closed" => "Fermée",
            _ => status ?? string.Empty,
        };

        public static string Priority(string? priority) => priority switch
        {
            "low" => "Basse",
            "normal" => "Normale",
            "high" => "Haute",
            "urgent" => "Urgente",
            _ => priority ?? string.Empty,
        };

        public static string Channel(string? channel) => channel switch
        {
            "agent" => "Saisie par l'équipe",
            "phone" => "Téléphone",
            "counter" => "Comptoir",
            "email" => "Courriel",
            "web" => "Site",
            "app" => "Application",
            "pos" => "Caisse",
            _ => channel ?? string.Empty,
        };

        /// <summary>Transitions permises par le serveur (contrat lot 1) : l'appli ne propose que celles-là.</summary>
        public static IReadOnlyList<string> NextStatuses(string status) => status switch
        {
            "new" => new[] { "open", "closed" },
            "open" => new[] { "waiting_customer", "closed" },
            "waiting_customer" => new[] { "open", "closed" },
            "closed" => new[] { "open" },
            _ => Array.Empty<string>(),
        };

        public static string Date(string? iso) =>
            DateTimeOffset.TryParse(iso, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
                ? date.LocalDateTime.ToString("d MMM yyyy", CultureInfo.CurrentCulture)
                : string.Empty;

        public static string DateTime(string? iso) =>
            DateTimeOffset.TryParse(iso, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
                ? date.LocalDateTime.ToString("d MMM yyyy, HH:mm", CultureInfo.CurrentCulture)
                : string.Empty;

        /// <summary>Message d'erreur du serveur traduit pour le gérant (codes du contrat lot 1).</summary>
        public static string? ErrorText(string? code) => code switch
        {
            "INVALID_STATUS_TRANSITION" => "Ce changement d'état n'est pas possible depuis l'état actuel.",
            "TICKET_CLOSED" => "Cette demande est fermée : rouvrez-la pour répondre.",
            "TICKET_NOT_FOUND" => "Cette demande n'existe plus.",
            "ATTACHMENT_TOO_LARGE" => "Pièce jointe trop lourde (10 Mo au plus).",
            _ => null,
        };
    }
}
