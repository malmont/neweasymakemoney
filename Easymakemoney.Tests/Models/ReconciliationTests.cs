using System.Globalization;
using Easymakemoney.Models;
using Easymakemoney.Services;
using Easymakemoney.Tests.Fakes;
using Easymakemoney.Utilities;
using Newtonsoft.Json;
using Xunit;

namespace Easymakemoney.Tests.Models;

[Collection("SiteCurrency")]
public class ReconciliationTests
{
    // Exemple réel du backend (demo, soldes de test supprimés ensuite).
    private const string Json = """
    {"currency":"CAD","tolerance":100,"items":[{"balanceId":3,"date":"2026-11-10","previousDate":"2026-10-31","expected":-1726322,"expectedAtEntry":-1726322,"actual":512000,"gap":2238322,"status":"open","decision":null,"suggestions":["read_receipt","add_expense","mark_unexplained"],"expenseWindow":{"from":"2026-11-01","to":"2026-11-10"},"composition":{"previous":{"id":2,"date":"2026-10-31","amount":500000},"plannedIn":0,"plannedOut":0,"recordedExpenses":{"total":2236555,"purchases":2210055,"shipping":11900,"notes":14600,"advertising":0},"receipts":{"total":10233,"cash":5749,"card":4484,"from":"2026-10-31","to":"2026-11-09"},"expected":-1726322,"days":10}}],"unexplained":{"total":0,"count":0}}
    """;

    [Fact]
    public void RealExample_IsReadWithGapCompositionAndWindow()
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("fr-CA");
            var list = JsonConvert.DeserializeObject<ReconciliationList>(Json)!.WithCurrency();
            var item = Assert.Single(list.items);

            Assert.True(item.IsOpen);
            Assert.Equal("Écart à traiter", item.StatusText);
            Assert.Contains("de plus que prévu", item.GapText);
            Assert.Contains("22", item.GapText);
            Assert.Contains("Solde du 31/10/2026", item.CompositionText);
            Assert.Contains("en espèces", item.CompositionText);
            Assert.Contains("entre le 01/11/2026 et le 10/11/2026", item.WindowText);
            Assert.Equal("Solde du 10/11/2026", item.Title);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
            SiteCurrency.Reset();
        }
    }

    [Fact]
    public void NegativeGap_SaysMoneyIsMissing_AndExplainedShowsTheNote()
    {
        var missing = new Reconciliation { gap = -32000, status = "explained", decision = new ReconciliationDecision { kind = "explained", note = "Dépôt d'espèces" } };

        Assert.Contains("de moins que prévu", missing.GapText);
        Assert.Equal("Expliqué : Dépôt d'espèces", missing.StatusText);
        Assert.False(missing.IsOpen);
    }

    [Fact]
    public async Task Service_UsesContractRoutes()
    {
        var fakeHttp = new FakeHttpService { ResultToReturn = new ReconciliationList() };
        var service = new TreasuryService(fakeHttp);

        await service.GetReconciliationsAsync(5);
        fakeHttp.ResultToReturn = null;
        await service.DecideGapAsync(3, "explained", "  Dépôt d'espèces  ");

        Assert.Equal("gestion/tresorerie/rapprochements?limit=5", Assert.Single(fakeHttp.RequestedUrls));
        var (url, body) = Assert.Single(fakeHttp.PostedAuthData);
        Assert.Equal("gestion/tresorerie/soldes/3/ecart", url);
        Assert.Equal("{\"decision\":\"explained\",\"note\":\"Dépôt d'espèces\"}", JsonConvert.SerializeObject(body));
    }
}
