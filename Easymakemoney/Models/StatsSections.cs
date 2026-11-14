using System.Globalization;
using Newtonsoft.Json.Linq;

namespace Easymakemoney.Models
{
    /// <summary>
    /// Volets des statistiques unifiées (lot backend S2, docs/statistiques-gestion.md § 6) : demandés par sections=payments,expenses
    /// sur la même route, avec la même plage et les mêmes comparaisons. Le serveur calcule tout ; l'application n'additionne rien.
    /// </summary>
    public class StatsSections
    {
        public StatsPaymentsSection? payments { get; set; }
        public StatsExpensesSection? expenses { get; set; }
        public StatsStockSection? stock { get; set; }
        public StatsShippingSection? shipping { get; set; }
        public StatsTaxesSection? taxes { get; set; }
    }

    /// <summary>Mouvements de stock de la plage, valeur du stock aujourd'hui et stock bas (lot S2-b).</summary>
    public class StatsStockSection
    {
        public StatsStockIndicators indicators { get; set; } = new();
        public StatsStockValue? value { get; set; }
        public int? lowCount { get; set; }
        public StatsSectionChart? chart { get; set; }
        public List<StatsCaveat> caveats { get; set; } = new();
        public bool appliesSource { get; set; }
    }

    public class StatsStockIndicators
    {
        public StatsIndicator? unitsIn { get; set; }
        public StatsIndicator? unitsOut { get; set; }
        public StatsIndicator? receptions { get; set; }
        public StatsIndicator? adjustments { get; set; }
        public StatsIndicator? returns { get; set; }
    }

    public class StatsStockValue
    {
        public double? saleValue { get; set; }
        public double? costValue { get; set; }
        public string? historyStartsAt { get; set; }
    }

    /// <summary>Port facturé aux clients, par transporteur (lot S2-b) ; ce n'est pas le coût des étiquettes.</summary>
    public class StatsShippingSection
    {
        public StatsShippingIndicators indicators { get; set; } = new();
        public List<StatsCarrier> breakdown { get; set; } = new();
        public StatsSectionChart? chart { get; set; }
        public List<StatsCaveat> caveats { get; set; } = new();
        public bool appliesSource { get; set; } = true;
    }

    public class StatsShippingIndicators
    {
        public StatsIndicator? billed { get; set; }
        public StatsIndicator? orders { get; set; }
        public StatsIndicator? averagePerOrder { get; set; }
    }

    public class StatsCarrier
    {
        public int? carrierId { get; set; }
        public string? name { get; set; }
        public string? kind { get; set; }
        public double? billed { get; set; }
        public int? orders { get; set; }
        public double? share { get; set; }
    }

    /// <summary>Taxes collectées, nettes des retours, taxe par taxe (lot S2-b) ; aide à préparer une déclaration, n'en est pas une.</summary>
    public class StatsTaxesSection
    {
        public StatsTaxesIndicators indicators { get; set; } = new();
        public List<StatsTax> breakdown { get; set; } = new();
        public StatsSectionChart? chart { get; set; }
        public List<StatsCaveat> caveats { get; set; } = new();
        public bool appliesSource { get; set; } = true;
    }

    public class StatsTaxesIndicators
    {
        public StatsIndicator? collected { get; set; }
    }

    public class StatsTax
    {
        public string? code { get; set; }
        public string? label { get; set; }
        public string? jurisdiction { get; set; }
        public long? ratePpm { get; set; }
        public double? @base { get; set; }
        public double? collected { get; set; }
        public double? share { get; set; }
    }

    /// <summary>Encaissé, remboursé et net (datés par la date de la commande, comme « Ma journée »), par moyen de paiement, et les clôtures de caisse.</summary>
    public class StatsPaymentsSection
    {
        public StatsPaymentsIndicators indicators { get; set; } = new();
        public List<StatsPaymentMethod> breakdown { get; set; } = new();
        public StatsCashDesk? cashDesk { get; set; }
        public StatsSectionChart? chart { get; set; }
        public List<StatsCaveat> caveats { get; set; } = new();
        public bool appliesSource { get; set; } = true;
    }

    public class StatsPaymentsIndicators
    {
        public StatsIndicator? received { get; set; }
        public StatsIndicator? refunded { get; set; }
        public StatsIndicator? net { get; set; }
    }

    public class StatsPaymentMethod
    {
        public string? method { get; set; }
        public string? label { get; set; }
        public double? received { get; set; }
        public double? refunded { get; set; }
        public double? net { get; set; }
        public double? share { get; set; }
        public double? previousNet { get; set; }
        public double? lastYearNet { get; set; }
    }

    /// <summary>Clôtures de caisse de la plage : rendu pour les canaux « tout » et « caisse », null sinon.</summary>
    public class StatsCashDesk
    {
        public StatsIndicator? closings { get; set; }
        public StatsIndicator? discrepancy { get; set; }
        public StatsIndicator? bankDeposits { get; set; }
        public int discrepancyCount { get; set; }
        public StatsCashMovements? movements { get; set; }
    }

    public class StatsCashMovements
    {
        public double? deposits { get; set; }
        public double? withdrawals { get; set; }
        public double? fundDeposits { get; set; }
        public double? fundWithdrawals { get; set; }
    }

    /// <summary>Notes de frais, port d'achat et publicité (le total égale les dépenses du résultat de période) ; les frais fixes à côté, hors total.</summary>
    public class StatsExpensesSection
    {
        public StatsExpensesIndicators indicators { get; set; } = new();
        public List<StatsExpenseKind> breakdown { get; set; } = new();
        public List<StatsNoteType> notesByType { get; set; } = new();
        public List<StatsCampaign> campaigns { get; set; } = new();
        public StatsFixedCosts? fixedCosts { get; set; }
        public StatsSectionChart? chart { get; set; }
        public List<StatsCaveat> caveats { get; set; } = new();
        public bool appliesSource { get; set; }
    }

    public class StatsExpensesIndicators
    {
        public StatsIndicator? total { get; set; }
        public StatsIndicator? notes { get; set; }
        public StatsIndicator? purchaseShipping { get; set; }
        public StatsIndicator? advertising { get; set; }
    }

    public class StatsExpenseKind
    {
        public string? kind { get; set; }
        public string? label { get; set; }
        public double? amount { get; set; }
        public double? share { get; set; }
    }

    public class StatsNoteType
    {
        public int? typeId { get; set; }
        public string? name { get; set; }
        public double? amount { get; set; }
        public double? share { get; set; }
    }

    public class StatsCampaign
    {
        public int? id { get; set; }
        public string? name { get; set; }
        public double? amount { get; set; }
        public string? startDate { get; set; }
    }

    public class StatsFixedCosts : StatsIndicator
    {
        public JToken? byCategory { get; set; }
        public int? days { get; set; }
        public bool includedInTotal { get; set; }
    }

    /// <summary>Graphique d'un volet : mêmes tranches que le graphique principal (mêmes key, from, to).</summary>
    public class StatsSectionChart
    {
        public string? granularity { get; set; }
        public List<StatsSectionPoint> points { get; set; } = new();
    }

    public class StatsSectionPoint
    {
        public string? key { get; set; }
        public string? from { get; set; }
        public string? to { get; set; }
        public double? received { get; set; }
        public double? refunded { get; set; }
        public double? net { get; set; }
        public double? total { get; set; }
        public double? saleValue { get; set; }
        public double? billed { get; set; }
        public double? collected { get; set; }
        public JObject? lastYear { get; set; }

        public double? LastYearValue(string field) => lastYear?[field] is { Type: JTokenType.Integer or JTokenType.Float } value ? value.Value<double>() : null;
    }

    /// <summary>Une ligne « libellé · montant · détail » d'un volet.</summary>
    public sealed record StatsSectionRow(string Title, string ValueText, string Detail = "")
    {
        public bool HasDetail => Detail.Length > 0;
    }

    /// <summary>Un volet prêt à afficher : ses cartes, ses lignes de détail et ses remarques.</summary>
    public sealed class StatsSectionView
    {
        public List<StatsCardRow> CardRows { get; init; } = new();
        public List<StatsSectionRow> Rows { get; init; } = new();
        public string RowsTitle { get; init; } = string.Empty;
        public List<StatsSectionRow> ExtraRows { get; init; } = new();
        public string ExtraTitle { get; init; } = string.Empty;
        public string CaveatsText { get; init; } = string.Empty;

        public bool HasRows => Rows.Count > 0;
        public bool HasExtra => ExtraRows.Count > 0;
        public bool HasCaveats => CaveatsText.Length > 0;

        public static string Caveats(IEnumerable<StatsCaveat> caveats) =>
            string.Join("\n", caveats.Where(c => !string.IsNullOrWhiteSpace(c.text)).Select(c => $"• {c.text}"));

        internal static string Share(double? share) =>
            share is { } s ? s.ToString("0.#", CultureInfo.CurrentCulture) + " %" : string.Empty;

        /// <summary>« Paiements et caisse » : trois cartes, le détail par moyen de paiement, puis les clôtures de caisse s'il y en a.</summary>
        public static StatsSectionView? Payments(StatsPaymentsSection? section, string? currency, bool hasPrevious)
        {
            if (section == null)
                return null;
            var cards = new List<StatsCard>();
            void Add(string key, string title, string term, StatsIndicator? indicator, bool? higherIsBetter)
            {
                if (indicator != null)
                    cards.Add(new StatsCard(key, title, term, indicator, higherIsBetter, currency, hasPrevious));
            }
            Add("received", "Encaissé", "encaisse", section.indicators.received, true);
            Add("refunded", "Remboursé", "rembourse", section.indicators.refunded, false);
            Add("net", "Encaissé net", "encaisse-net", section.indicators.net, true);

            var methods = section.breakdown.Select(m =>
            {
                var details = new List<string>();
                if (m.received is { } received && m.refunded is { } refunded && refunded != 0)
                    details.Add($"{StatsFormat.Value(received, "money", currency)} encaissés, {StatsFormat.Value(refunded, "money", currency)} remboursés");
                if (Share(m.share) is { Length: > 0 } share)
                    details.Add($"{share} du net");
                return new StatsSectionRow(m.label ?? m.method ?? "—", StatsFormat.Value(m.net, "money", currency), string.Join(" · ", details));
            }).ToList();

            var desk = new List<StatsSectionRow>();
            if (section.cashDesk is { } cash)
            {
                desk.Add(new StatsSectionRow("Clôtures", StatsFormat.Value(cash.closings?.value, "count", currency)));
                var count = cash.discrepancyCount;
                desk.Add(new StatsSectionRow("Écart de caisse", Signed(cash.discrepancy?.value, currency),
                    count == 0 ? "Aucune clôture avec écart" : $"{count} clôture{(count > 1 ? "s" : string.Empty)} avec écart"));
                desk.Add(new StatsSectionRow("Dépôts en banque", StatsFormat.Value(cash.bankDeposits?.value, "money", currency)));
                if (cash.movements is { } m)
                {
                    AddMovement(desk, "Fond de caisse ajouté", m.fundDeposits, currency);
                    AddMovement(desk, "Fond de caisse retiré", m.fundWithdrawals, currency);
                    AddMovement(desk, "Ajouts d'argent hors ventes", m.deposits, currency);
                    AddMovement(desk, "Retraits d'argent hors ventes", m.withdrawals, currency);
                }
            }

            return new StatsSectionView
            {
                CardRows = StatsCardRow.Pair(cards),
                Rows = methods,
                RowsTitle = "Par moyen de paiement",
                ExtraRows = desk,
                ExtraTitle = "Clôtures de caisse",
                CaveatsText = Caveats(section.caveats),
            };
        }

        /// <summary>« Frais » : quatre cartes, les notes par type, les campagnes, puis les frais fixes à côté (jamais ajoutés au total).</summary>
        public static StatsSectionView? Expenses(StatsExpensesSection? section, string? currency, bool hasPrevious)
        {
            if (section == null)
                return null;
            var cards = new List<StatsCard>();
            void Add(string key, string title, string term, StatsIndicator? indicator)
            {
                if (indicator != null)
                    cards.Add(new StatsCard(key, title, term, indicator, false, currency, hasPrevious));
            }
            Add("expensesTotal", "Total des frais", "frais", section.indicators.total);
            Add("notes", "Notes de frais", "note-de-frais", section.indicators.notes);
            Add("purchaseShipping", "Port d'achat", "port-achat", section.indicators.purchaseShipping);
            Add("advertising", "Publicité", "publicite", section.indicators.advertising);

            var rows = section.notesByType
                .Select(n => new StatsSectionRow(n.name ?? "Sans type", StatsFormat.Value(n.amount, "money", currency),
                    Share(n.share) is { Length: > 0 } share ? $"{share} des notes" : string.Empty))
                .Concat(section.campaigns.Select(c => new StatsSectionRow(
                    $"Campagne : {c.name ?? "sans nom"}", StatsFormat.Value(c.amount, "money", currency))))
                .ToList();

            var extra = new List<StatsSectionRow>();
            if (section.fixedCosts is { } fixedCosts && !fixedCosts.includedInTotal && (fixedCosts.value ?? 0) != 0)
            {
                var days = fixedCosts.days is { } d ? $"Part de {d} jour{(d > 1 ? "s" : string.Empty)}, " : string.Empty;
                extra.Add(new StatsSectionRow("Frais fixes", StatsFormat.Value(fixedCosts.value, "money", currency), $"{days}hors du total des frais"));
            }

            return new StatsSectionView
            {
                CardRows = StatsCardRow.Pair(cards),
                Rows = rows,
                RowsTitle = "Notes de frais par type et campagnes",
                ExtraRows = extra,
                ExtraTitle = "À côté du total",
                CaveatsText = Caveats(section.caveats),
            };
        }

        /// <summary>« Stock » : les cinq mouvements de la plage, puis la valeur d'aujourd'hui et le stock bas.</summary>
        public static StatsSectionView? Stock(StatsStockSection? section, string? currency, bool hasPrevious)
        {
            if (section == null)
                return null;
            var cards = new List<StatsCard>();
            void Add(string key, string title, string term, StatsIndicator? indicator)
            {
                if (indicator != null)
                    cards.Add(new StatsCard(key, title, term, indicator, null, currency, hasPrevious));
            }
            Add("unitsOut", "Sorties", "sorties-stock", section.indicators.unitsOut);
            Add("unitsIn", "Entrées", "entrees-stock", section.indicators.unitsIn);
            Add("receptions", "Réceptions fournisseur", "receptions-stock", section.indicators.receptions);
            Add("returns", "Retours", "retours-stock", section.indicators.returns);
            Add("adjustments", "Ajustements", "ajustements-stock", section.indicators.adjustments);

            var rows = new List<StatsSectionRow>();
            if (section.value is { } value)
            {
                rows.Add(new StatsSectionRow("Valeur de vente aujourd'hui", StatsFormat.Value(value.saleValue, "money", currency)));
                rows.Add(new StatsSectionRow("Valeur au coût aujourd'hui", StatsFormat.Value(value.costValue, "money", currency)));
            }
            if (section.lowCount is { } low)
                rows.Add(new StatsSectionRow("Articles en stock bas", StatsFormat.Value(low, "count", currency)));

            return new StatsSectionView
            {
                CardRows = StatsCardRow.Pair(cards),
                Rows = rows,
                RowsTitle = "Le stock aujourd'hui",
                CaveatsText = Caveats(section.caveats),
            };
        }

        /// <summary>« Livraison » : port facturé, ventes livrées, moyenne, puis le détail par transporteur.</summary>
        public static StatsSectionView? Shipping(StatsShippingSection? section, string? currency, bool hasPrevious)
        {
            if (section == null)
                return null;
            var cards = new List<StatsCard>();
            void Add(string key, string title, string term, StatsIndicator? indicator, bool? higherIsBetter)
            {
                if (indicator != null)
                    cards.Add(new StatsCard(key, title, term, indicator, higherIsBetter, currency, hasPrevious));
            }
            Add("shippingBilled", "Port facturé", "port-facture", section.indicators.billed, true);
            Add("shippingOrders", "Ventes livrées", "commandes", section.indicators.orders, true);
            Add("shippingAverage", "Port moyen", "port-moyen", section.indicators.averagePerOrder, null);

            var rows = section.breakdown.Select(c =>
            {
                var details = new List<string>();
                if (c.orders is { } orders)
                    details.Add($"{orders} vente{(orders > 1 ? "s" : string.Empty)}");
                if (Share(c.share) is { Length: > 0 } share)
                    details.Add($"{share} du port");
                return new StatsSectionRow(c.name ?? "Sans transporteur", StatsFormat.Value(c.billed, "money", currency), string.Join(" · ", details));
            }).ToList();

            return new StatsSectionView
            {
                CardRows = StatsCardRow.Pair(cards),
                Rows = rows,
                RowsTitle = "Par transporteur",
                CaveatsText = Caveats(section.caveats),
            };
        }

        /// <summary>« Taxes » : le total collecté, puis chaque taxe avec son taux et sa base quand elle est connue.</summary>
        public static StatsSectionView? Taxes(StatsTaxesSection? section, string? currency, bool hasPrevious)
        {
            if (section == null)
                return null;
            var cards = new List<StatsCard>();
            if (section.indicators.collected is { } collected)
                cards.Add(new StatsCard("taxesCollected", "Taxes collectées", "taxes-collectees", collected, null, currency, hasPrevious));

            var rows = section.breakdown.Select(t =>
            {
                var details = new List<string>();
                if (t.ratePpm is { } ppm)
                    details.Add($"taux {(ppm / 10000.0).ToString("0.###", CultureInfo.CurrentCulture)} %");
                if (t.@base is { } taxBase)
                    details.Add($"base {StatsFormat.Value(taxBase, "money", currency)}");
                if (Share(t.share) is { Length: > 0 } share)
                    details.Add($"{share} des taxes");
                return new StatsSectionRow(t.label ?? t.code ?? "Taxe", StatsFormat.Value(t.collected, "money", currency), string.Join(" · ", details));
            }).ToList();

            return new StatsSectionView
            {
                CardRows = StatsCardRow.Pair(cards),
                Rows = rows,
                RowsTitle = "Par taxe",
                CaveatsText = Caveats(section.caveats),
            };
        }

        private static void AddMovement(List<StatsSectionRow> rows, string title, double? amount, string? currency)
        {
            if ((amount ?? 0) != 0)
                rows.Add(new StatsSectionRow(title, StatsFormat.Value(amount, "money", currency)));
        }

        /// <summary>« −0,25 $ » ou « +3,00 $ » : un écart de caisse garde son signe.</summary>
        internal static string Signed(double? value, string? currency)
        {
            if (value is not { } v)
                return "—";
            var text = StatsFormat.Value(Math.Abs(v), "money", currency);
            return v > 0 ? $"+{text}" : v < 0 ? $"−{text}" : text;
        }
    }
}
