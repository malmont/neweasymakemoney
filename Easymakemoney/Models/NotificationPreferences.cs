namespace Easymakemoney.Models
{
    /// <summary>
    /// Niveaux des courriels de suivi (lot backend NOTIF-1, docs/notifications-courriels.md) : GET/PATCH /api/gestion/notifications/preferences.
    /// Le serveur rédige les libellés ; l'application affiche et envoie le choix.
    /// </summary>
    public class NotificationPreferences
    {
        public NotificationLevel? level { get; set; }
        public List<NotificationKind> kinds { get; set; } = new();

        public string SiteLevelText => level?.siteLevel is { } site
            ? $"Niveau du site : {site.label ?? site.value}{(string.IsNullOrWhiteSpace(site.reasonLabel) ? string.Empty : $" ({site.reasonLabel})")}"
            : string.Empty;

        public string CapText => level?.dailyCap is { } cap ? $"Au plus {cap} courriels de suivi par jour." : string.Empty;

        public string HeldText => level?.heldCount is > 0 and var n
            ? $"{n} courriel{(n > 1 ? "s" : string.Empty)} retenu{(n > 1 ? "s" : string.Empty)} pour le prochain résumé."
            : string.Empty;

        public bool HasHeld => HeldText.Length > 0;

        /// <summary>« Comme le site » puis les quatre niveaux, dans l'ordre du serveur ; le choix actuel est coché.</summary>
        public List<NotificationLevelChoice> Choices()
        {
            var current = level?.userLevel;
            var list = new List<NotificationLevelChoice>
            {
                new(null, level?.siteChoiceLabel ?? "Comme le site", SiteLevelText, current == null),
            };
            foreach (var choice in level?.choices ?? new())
                list.Add(new NotificationLevelChoice(choice.key, choice.label ?? choice.key ?? string.Empty, choice.description ?? string.Empty, current != null && current == choice.key));
            return list;
        }
    }

    public class NotificationLevel
    {
        public string? value { get; set; }
        public string? source { get; set; }
        public string? userLevel { get; set; }
        public NotificationSiteLevel? siteLevel { get; set; }
        public int? dailyCap { get; set; }
        public int heldCount { get; set; }
        public string? siteChoiceLabel { get; set; }
        public List<NotificationChoice> choices { get; set; } = new();
    }

    public class NotificationSiteLevel
    {
        public string? value { get; set; }
        public string? mode { get; set; }
        public string? reason { get; set; }
        public string? label { get; set; }
        public string? reasonLabel { get; set; }
    }

    public class NotificationChoice
    {
        public string? key { get; set; }
        public string? label { get; set; }
        public string? description { get; set; }
    }

    public class NotificationKind
    {
        public string? kind { get; set; }
        public string? label { get; set; }
        public bool enabled { get; set; }
        public bool available { get; set; } = true;
        public string? channel { get; set; }
        public string? family { get; set; }

        public string ChannelText => channel == "email" ? "Courriel" : "Téléphone";
    }

    public sealed partial class NotificationLevelChoice : ObservableObject
    {
        public NotificationLevelChoice(string? key, string label, string description, bool selected)
        {
            Key = key;
            Label = label;
            Description = description;
            _isSelected = selected;
        }

        public string? Key { get; }
        public string Label { get; }
        public string Description { get; }
        public bool HasDescription => Description.Length > 0;

        [ObservableProperty]
        private bool _isSelected;
    }
}
