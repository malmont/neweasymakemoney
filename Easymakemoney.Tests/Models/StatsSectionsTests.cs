using System.Globalization;
using Easymakemoney.Models;
using Easymakemoney.Services;
using Easymakemoney.Tests.Fakes;
using Easymakemoney.Utilities;
using Newtonsoft.Json;
using Xunit;

namespace Easymakemoney.Tests.Models;

/// <summary>Volets « Paiements et caisse » et « Frais » des statistiques unifiées (lot backend S2-a), sur l'exemple réel de demo d'octobre 2026.</summary>
[Collection("SiteCurrency")]
public class StatsSectionsTests : IDisposable
{
    private readonly CultureInfo _originalCulture = CultureInfo.CurrentCulture;

    public StatsSectionsTests()
    {
        CultureInfo.CurrentCulture = new CultureInfo("fr-CA");
        SiteCurrency.Reset();
    }

    public void Dispose()
    {
        CultureInfo.CurrentCulture = _originalCulture;
        SiteCurrency.Reset();
    }

    private const string Json = """
    { "currency": "CAD", "source": "all",
      "previous": { "from": "2026-09-01", "to": "2026-09-30" },
      "indicators": { "revenue": { "value": 191649, "unit": "money" } },
      "chart": { "granularity": "day", "points": [] },
      "caveats": [],
      "sections": {
        "payments": {
          "indicators": {
            "received": { "value": 191649, "unit": "money", "previous": 0, "lastYear": null, "changeVsPrevious": null, "changeVsLastYear": null, "changeUnit": "percent", "spark": [0, 31618, 36522] },
            "refunded": { "value": 15064, "unit": "money", "previous": 0, "changeUnit": "percent" },
            "net": { "value": 176585, "unit": "money", "previous": 0, "changeUnit": "percent" } },
          "breakdown": [
            { "method": "card", "label": "Carte", "received": 83241, "refunded": 0, "net": 83241, "share": 47.1, "previousNet": 0, "lastYearNet": null },
            { "method": "cash", "label": "Espèces", "received": 108408, "refunded": 10530, "net": 97878, "share": 55.4, "previousNet": 0, "lastYearNet": null },
            { "method": "other", "label": "Autre", "received": null, "refunded": null, "net": -4534, "share": -2.6, "previousNet": null, "lastYearNet": null } ],
          "cashDesk": {
            "closings": { "value": 1, "unit": "count", "previous": 0, "lastYear": null },
            "discrepancy": { "value": -25, "unit": "money" },
            "discrepancyCount": 1,
            "bankDeposits": { "value": 32419, "unit": "money" },
            "movements": { "deposits": 0, "withdrawals": 0, "fundDeposits": 10000, "fundWithdrawals": 0 } },
          "chart": { "granularity": "day", "points": [
            { "key": "2026-10-08", "from": "2026-10-08", "to": "2026-10-08", "received": 31618, "refunded": 0, "net": 31618, "lastYear": { "received": 0, "refunded": 0, "net": 0 } },
            { "key": "2026-10-09", "from": "2026-10-09", "to": "2026-10-09", "received": 36522, "refunded": 0, "net": 36522, "lastYear": null } ] },
          "caveats": [
            { "code": "PAYMENTS_BY_ORDER_DATE", "text": "L'encaissé est rangé au jour de la commande." },
            { "code": "REFUNDS_UNMATCHED", "text": "45,34 $ de remboursements sans ligne de paiement.", "amount": -4534 } ],
          "appliesSource": true },
        "expenses": {
          "indicators": {
            "total": { "value": 69200, "unit": "money", "previous": 0, "changeUnit": "percent" },
            "notes": { "value": 44300, "unit": "money" },
            "purchaseShipping": { "value": 24900, "unit": "money" },
            "advertising": { "value": 0, "unit": "money" } },
          "breakdown": [
            { "kind": "notes", "label": "Notes de frais", "amount": 44300, "share": 64 },
            { "kind": "purchaseShipping", "label": "Port d'achat", "amount": 24900, "share": 36 },
            { "kind": "advertising", "label": "Publicité", "amount": 0, "share": 0 } ],
          "notesByType": [
            { "typeId": 3, "name": "Hébergement", "amount": 32000, "share": 72.2 },
            { "typeId": 4, "name": "Fournitures", "amount": 12300, "share": 27.8 } ],
          "campaigns": [],
          "fixedCosts": { "value": 0, "previous": 0, "lastYear": null, "byCategory": [], "days": 31, "includedInTotal": false },
          "chart": { "granularity": "day", "points": [
            { "key": "2026-10-05", "from": "2026-10-05", "to": "2026-10-05", "total": 24900, "notes": 0, "purchaseShipping": 24900, "advertising": 0, "lastYear": { "total": 0 } } ] },
          "caveats": [],
          "appliesSource": false } } }
    """;

    private static UnifiedStatistics Read() => JsonConvert.DeserializeObject<UnifiedStatistics>(Json)!;

    private static string Money(double cents) => SiteCurrency.FormatCents(cents, null, "CAD");

    [Fact]
    public void Payments_CardsMethodsAndCashDesk()
    {
        var data = Read();
        var view = StatsSectionView.Payments(data.sections?.payments, data.currency, data.HasPrevious)!;

        var cards = view.CardRows.SelectMany(r => new[] { r.Left, r.Right }).Where(c => c != null).Select(c => c!).ToList();
        Assert.Equal(new[] { "received", "refunded", "net" }, cards.Select(c => c.Key));
        Assert.Equal(Money(176585), cards[2].ValueText);
        Assert.Equal(-1, StatsFormat.Tone(10, false)); // un remboursement qui monte est une mauvaise nouvelle

        Assert.Equal(new[] { "Carte", "Espèces", "Autre" }, view.Rows.Select(r => r.Title));
        Assert.Equal(Money(97878), view.Rows[1].ValueText);
        Assert.Contains("remboursés", view.Rows[1].Detail);
        Assert.Contains("55,4 %", view.Rows[1].Detail);
        Assert.Equal("−" + Money(4534), view.Rows[2].ValueText.Replace("-", "−"));
        Assert.DoesNotContain("encaissés", view.Rows[2].Detail); // « Autre » : ni encaissé ni remboursé connus

        Assert.Equal("Clôtures de caisse", view.ExtraTitle);
        Assert.Equal(new[] { "Clôtures", "Écart de caisse", "Dépôts en banque", "Fond de caisse ajouté" }, view.ExtraRows.Select(r => r.Title));
        Assert.Equal("−" + Money(25), view.ExtraRows[1].ValueText);
        Assert.Equal("1 clôture avec écart", view.ExtraRows[1].Detail);
        Assert.Equal(Money(32419), view.ExtraRows[2].ValueText);

        Assert.Contains("jour de la commande", view.CaveatsText);
        Assert.Contains("sans ligne de paiement", view.CaveatsText);
    }

    [Fact]
    public void Payments_WithoutCashDesk_HasNoClosings()
    {
        var data = Read();
        data.sections!.payments!.cashDesk = null; // canal boutique en ligne ou application
        var view = StatsSectionView.Payments(data.sections.payments, data.currency, data.HasPrevious)!;
        Assert.False(view.HasExtra);
    }

    [Fact]
    public void Expenses_CardsNotesAndFixedCostsBesideTheTotal()
    {
        var data = Read();
        var view = StatsSectionView.Expenses(data.sections?.expenses, data.currency, data.HasPrevious)!;

        var cards = view.CardRows.SelectMany(r => new[] { r.Left, r.Right }).Where(c => c != null).Select(c => c!).ToList();
        Assert.Equal(new[] { "expensesTotal", "notes", "purchaseShipping", "advertising" }, cards.Select(c => c.Key));
        Assert.Equal(Money(69200), cards[0].ValueText);

        Assert.Equal(new[] { "Hébergement", "Fournitures" }, view.Rows.Select(r => r.Title));
        Assert.Equal("72,2 % des notes", view.Rows[0].Detail);
        Assert.False(view.HasExtra); // frais fixes à 0 : rien à montrer

        data.sections!.expenses!.fixedCosts!.value = 37200;
        view = StatsSectionView.Expenses(data.sections.expenses, data.currency, data.HasPrevious)!;
        var fixedCosts = Assert.Single(view.ExtraRows);
        Assert.Equal("Frais fixes", fixedCosts.Title);
        Assert.Equal("Part de 31 jours, hors du total des frais", fixedCosts.Detail);
        Assert.Equal(Money(69200), cards[0].ValueText); // le total ne bouge pas
    }

    [Fact]
    public void ChartPoints_ForSections_UseTheirOwnChart()
    {
        var data = Read();

        var received = data.ChartPoints("received");
        Assert.Equal(2, received.Count);
        Assert.Equal(31618, received[0].Value);
        Assert.Equal(0, received[0].LastYear);
        Assert.Null(received[1].LastYear);

        var expenses = Assert.Single(data.ChartPoints("expenses"));
        Assert.Equal(24900, expenses.Value);
        Assert.Equal("2026-10-05", expenses.Key);

        Assert.Empty(new UnifiedStatistics().ChartPoints("received")); // volet absent : graphique vide, pas celui des ventes
    }

    [Fact]
    public void WithoutSections_NoView()
    {
        Assert.Null(StatsSectionView.Payments(null, "CAD", true));
        Assert.Null(StatsSectionView.Expenses(null, "CAD", true));
    }

    [Fact]
    public async Task Query_AsksForSectionsAtTheEnd()
    {
        var fakeHttp = new FakeHttpService { ResultToReturn = new UnifiedStatistics() };
        var service = new UnifiedStatisticsService(fakeHttp);

        await service.GetAsync(new StatsQuery("month", Sections: "payments,expenses"));

        Assert.Equal("gestion/statistiques?range=month&source=all&locale=fr&sections=payments,expenses", Assert.Single(fakeHttp.RequestedUrls));
    }
}
