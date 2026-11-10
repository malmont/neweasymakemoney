using Easymakemoney.Utilities;

namespace Easymakemoney.Models
{
    /// <summary>Budget par poste d'une période (GET periodes/{id}/budget, lot backend B1a, docs/budgets-gestion.md).</summary>
    public class PeriodBudget
    {
        public PeriodBudgetPeriod? period { get; set; }
        public string? currency { get; set; }
        public long? globalBudget { get; set; }
        public List<PeriodBudgetLine> lines { get; set; } = new();
        public PeriodBudgetLine? total { get; set; }

        /// <summary>Donne la devise du budget aux lignes, qui ne la portent pas elles-mêmes.</summary>
        public PeriodBudget WithCurrency()
        {
            foreach (var line in lines)
                line.Currency = currency;
            if (total != null)
            {
                total.Currency = currency;
                total.label ??= "Total";
            }
            return this;
        }

        /// <summary>
        /// Le total ne se compare au prévu que si chaque poste a un prévu : sinon, les dépenses des postes sans prévu
        /// feraient paraître le total « dépassé ».
        /// </summary>
        public bool TotalComparable => lines.Count > 0 && lines.All(line => line.HasPlanned);

        public bool ShowTotalHint => !TotalComparable && (total?.HasPlanned ?? false);

        public bool HasGlobalBudget => globalBudget is > 0;
        public string GlobalBudgetText => $"Budget global de la période : {SiteCurrency.FormatCents(globalBudget ?? 0, null, currency)}";
    }

    public class PeriodBudgetPeriod
    {
        public int id { get; set; }
        public string? name { get; set; }
        public string? periodKey { get; set; }
    }

    public class PeriodBudgetLine
    {
        public string? category { get; set; }
        public string? label { get; set; }
        public long planned { get; set; }
        public long actual { get; set; }
        public long remaining { get; set; }
        public double? ratio { get; set; }
        public string? status { get; set; }
        public List<PeriodBudgetDetail>? details { get; set; }

        [JsonIgnore]
        public string? Currency { get; set; }

        public bool HasPlanned => planned > 0;
        public string PlannedText => HasPlanned ? $"Prévu {Money(planned)}" : "Aucun prévu";
        public string ActualText => $"Dépensé {Money(actual)}";

        public string RemainingText => !HasPlanned
            ? string.Empty
            : remaining >= 0 ? $"Reste {Money(remaining)}" : $"Dépassé de {Money(-remaining)}";

        public string StatusText => status switch
        {
            "over" => "Dépassé",
            "warning" => "Bientôt atteint",
            "ok" => "Dans le prévu",
            _ => "Sans prévu",
        };

        public string StatusColorHex => status switch
        {
            "over" => "#B91C1C",
            "warning" => "#B45309",
            "ok" => "#047857",
            _ => "#64748B",
        };

        public Microsoft.Maui.Graphics.Color StatusColor => Microsoft.Maui.Graphics.Color.FromArgb(StatusColorHex);

        /// <summary>Remplissage de la barre, de 0 à 1 ; plein dès que le prévu est atteint.</summary>
        public double Progress => ratio is null ? 0 : Math.Clamp(ratio.Value, 0, 1);

        public string DetailsText => details == null || details.Count == 0
            ? string.Empty
            : string.Join(" · ", details.Select(detail => $"{detail.label} {Money(detail.actual)}"));

        public bool HasDetails => DetailsText.Length > 0;

        private string Money(long cents) => SiteCurrency.FormatCents(cents, null, Currency);
    }

    public class PeriodBudgetDetail
    {
        public string? key { get; set; }
        public string? label { get; set; }
        public long actual { get; set; }
    }
}
