using System.Text;

namespace Easymakemoney.Models
{
    /// <summary>
    /// Matériel de la Prévention des pertes (backend L3-a et L3-a2) : tableau de santé, boîtiers et jumelage par code.
    /// Le téléphone ne montre jamais une clé de boîtier : la création par clé, la nouvelle clé et la révocation restent sur le web.
    /// </summary>
    public class LossHealth
    {
        public List<LossHealthDevice> devices { get; set; } = new();
        public LossHealthSummary summary { get; set; } = new();
        public int thresholdSeconds { get; set; }
        public bool exercise { get; set; }
        public string? checkedAt { get; set; }
    }

    public class LossHealthDevice
    {
        public const string OkColor = "#15803D";
        public const string PendingColor = "#B45309";
        public const string DownColor = "#B91C1C";
        public const string AbsentColor = "#9CA3AF";

        public string code { get; set; } = string.Empty;
        public string kind { get; set; } = string.Empty;
        public string label { get; set; } = string.Empty;
        public string? zone { get; set; }
        public int? cashRegisterId { get; set; }
        public string state { get; set; } = string.Empty;
        public string stateLabel { get; set; } = string.Empty;
        public string? lastSeenAt { get; set; }
        public string? silentSince { get; set; }
        public int? silentSeconds { get; set; }
        public string? hint { get; set; }

        /// <summary>Pastille : vert en ordre, orange en attente, rouge en panne, gris pas installé.</summary>
        public string DotColor => state switch
        {
            "ok" => OkColor,
            "pending" => PendingColor,
            "down" => DownColor,
            _ => AbsentColor,
        };

        public bool HasHint => !string.IsNullOrWhiteSpace(hint);
    }

    public class LossHealthSummary
    {
        public int ok { get; set; }
        public int pending { get; set; }
        public int down { get; set; }
        public int absent { get; set; }
        public bool allOk { get; set; }
        public string label { get; set; } = string.Empty;

        public string BannerColor => allOk ? LossHealthDevice.OkColor : (down > 0 ? LossHealthDevice.DownColor : LossHealthDevice.PendingColor);
    }

    public class LossBoxList
    {
        public List<LossBox> items { get; set; } = new();
        public int total { get; set; }
        public int limit { get; set; }
    }

    public class LossBox
    {
        public int id { get; set; }
        public string name { get; set; } = string.Empty;
        public string? place { get; set; }
        public string status { get; set; } = string.Empty;
        public string statusLabel { get; set; } = string.Empty;
        public bool online { get; set; }
        public string? onlineLabel { get; set; }
        public string? lastSeenLabel { get; set; }
        public bool paired { get; set; }
        public bool exercise { get; set; }
        public List<LossBoxDevice> devices { get; set; } = new();

        public bool HasPlace => !string.IsNullOrWhiteSpace(place);

        public string StateText => string.IsNullOrWhiteSpace(onlineLabel) || onlineLabel == "—"
            ? statusLabel
            : $"{statusLabel} · {onlineLabel}";

        public string DevicesText => devices.Count == 0
            ? "Aucun appareil branché"
            : string.Join("\n", devices.Select(device => $"{device.kindLabel} : {device.statusLabel}"));
    }

    public class LossBoxDevice
    {
        public int id { get; set; }
        public string kind { get; set; } = string.Empty;
        public string kindLabel { get; set; } = string.Empty;
        public string status { get; set; } = string.Empty;
        public string statusLabel { get; set; } = string.Empty;
    }

    public class LossBoxPairResult
    {
        public LossBox? box { get; set; }
    }

    /// <summary>Code de jumelage d'un boîtier, de la forme KX7-42P-9QM : lu par le QR code ou tapé.</summary>
    public static class LossPairingCode
    {
        public const int Length = 9;

        /// <summary>
        /// Rend le code en majuscules, avec ses tirets, ou null s'il n'a pas 9 caractères.
        /// Le QR code peut porter une adresse avec « code= » : seul le code est gardé.
        /// </summary>
        public static string? Normalize(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return null;
            var text = raw.Trim();
            var marker = text.IndexOf("code=", StringComparison.OrdinalIgnoreCase);
            if (marker >= 0)
            {
                text = text[(marker + 5)..];
                var end = text.IndexOfAny(new[] { '&', '#' });
                if (end >= 0)
                    text = text[..end];
                text = Uri.UnescapeDataString(text);
            }

            var letters = new StringBuilder();
            foreach (var c in text)
            {
                if (char.IsAsciiLetterOrDigit(c))
                    letters.Append(char.ToUpperInvariant(c));
            }
            if (letters.Length != Length)
                return null;
            var code = letters.ToString();
            return $"{code[..3]}-{code[3..6]}-{code[6..]}";
        }
    }
}
