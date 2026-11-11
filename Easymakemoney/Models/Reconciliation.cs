using Easymakemoney.Utilities;

namespace Easymakemoney.Models
{
    /// <summary>
    /// Rapprochement d'un solde saisi avec ce que la trésorerie attendait (lot backend RAP, docs/tresorerie-gestion.md § 6).
    /// L'écart est « vivant » : une dépense oubliée saisie plus tard le réduit d'elle-même.
    /// </summary>
    public class Reconciliation
    {
        public int balanceId { get; set; }
        public string? date { get; set; }
        public string? previousDate { get; set; }
        public long expected { get; set; }
        public long actual { get; set; }
        public long gap { get; set; }
        public string? status { get; set; }
        public ReconciliationDecision? decision { get; set; }
        public List<string> suggestions { get; set; } = new();
        public ReconciliationWindow? expenseWindow { get; set; }
        public ReconciliationComposition? composition { get; set; }

        [JsonIgnore]
        public string? Currency { get; set; }

        public bool IsOpen => status == "open";
        public bool IsMatched => status == "matched";

        public string StatusText => status switch
        {
            "matched" => "Rapproché : pas d'écart",
            "unexplained" => "Écart non justifié",
            "explained" => decision?.note is { Length: > 0 } note ? $"Expliqué : {note}" : "Écart expliqué",
            _ => "Écart à traiter",
        };

        public Microsoft.Maui.Graphics.Color StatusColor => Microsoft.Maui.Graphics.Color.FromArgb(status switch
        {
            "matched" => "#047857",
            "explained" => "#047857",
            "unexplained" => "#B45309",
            _ => "#B91C1C",
        });

        /// <summary>Écart positif : plus d'argent que prévu ; négatif : il manque de l'argent (dépense oubliée ?).</summary>
        public string GapText => gap switch
        {
            0 => "Aucun écart",
            > 0 => $"+{Money(gap)} de plus que prévu",
            _ => $"{Money(-gap)} de moins que prévu",
        };

        public string Title => $"Solde du {TreasuryDates.Short(date)}";

        public string AmountsText => $"Attendu {Money(expected)} · saisi {Money(actual)}";

        /// <summary>Le détail de l'attendu, ligne par ligne, pour comprendre l'écart.</summary>
        public string CompositionText
        {
            get
            {
                if (composition == null)
                    return string.Empty;

                var c = composition;
                var lines = new List<string>
                {
                    $"Solde du {TreasuryDates.Short(c.previous?.date)} : {Money(c.previous?.amount ?? 0)}",
                };
                if (c.plannedIn != 0 || c.plannedOut != 0)
                    lines.Add($"Mouvements prévus : +{Money(c.plannedIn)} / −{Money(c.plannedOut)}");
                if (c.recordedExpenses is { total: not 0 } expenses)
                    lines.Add($"Dépenses saisies : −{Money(expenses.total)} (achats {Money(expenses.purchases)}, port {Money(expenses.shipping)}, notes {Money(expenses.notes)}, publicité {Money(expenses.advertising)})");
                if (c.receipts is { } receipts)
                    lines.Add(receipts.cash != 0
                        ? $"Encaissements du {TreasuryDates.Short(receipts.from)} au {TreasuryDates.Short(receipts.to)} : +{Money(receipts.total)}, dont {Money(receipts.cash)} en espèces (peut-être pas encore déposées)"
                        : $"Encaissements du {TreasuryDates.Short(receipts.from)} au {TreasuryDates.Short(receipts.to)} : +{Money(receipts.total)}");
                lines.Add($"Attendu : {Money(expected)}");
                return string.Join("\n", lines);
            }
        }

        public string WindowText => expenseWindow == null
            ? string.Empty
            : $"Une dépense oubliée datée entre le {TreasuryDates.Short(expenseWindow.from)} et le {TreasuryDates.Short(expenseWindow.to)} réduira l'écart.";

        private string Money(long cents) => SiteCurrency.FormatCents(cents, null, Currency);
    }

    public class ReconciliationDecision
    {
        public string? kind { get; set; }
        public string? note { get; set; }
        public string? by { get; set; }
        public string? at { get; set; }
    }

    public class ReconciliationWindow
    {
        public string? from { get; set; }
        public string? to { get; set; }
    }

    public class ReconciliationComposition
    {
        public TreasuryPoint? previous { get; set; }
        public long plannedIn { get; set; }
        public long plannedOut { get; set; }
        public ReconciliationExpenses? recordedExpenses { get; set; }
        public ReconciliationReceipts? receipts { get; set; }
        public long expected { get; set; }
        public int days { get; set; }
    }

    public class ReconciliationExpenses
    {
        public long total { get; set; }
        public long purchases { get; set; }
        public long shipping { get; set; }
        public long notes { get; set; }
        public long advertising { get; set; }
    }

    public class ReconciliationReceipts
    {
        public long total { get; set; }
        public long cash { get; set; }
        public long card { get; set; }
        public string? from { get; set; }
        public string? to { get; set; }
    }

    public class ReconciliationList
    {
        public string? currency { get; set; }
        public long tolerance { get; set; }
        public List<Reconciliation> items { get; set; } = new();
        public ReconciliationUnexplained? unexplained { get; set; }

        public ReconciliationList WithCurrency()
        {
            foreach (var item in items)
                item.Currency = currency;
            return this;
        }
    }

    public class ReconciliationUnexplained
    {
        public long total { get; set; }
        public int count { get; set; }
    }

    /// <summary>Résumé du rapprochement dans GET gestion/tresorerie.</summary>
    public class ReconciliationSummary
    {
        public string? lastStatus { get; set; }
        public long? lastGap { get; set; }
        public string? lastDate { get; set; }
        public long tolerance { get; set; }
        public ReconciliationUnexplained? unexplained { get; set; }
    }

    public class BalanceSaveResult
    {
        public Reconciliation? reconciliation { get; set; }
    }
}
