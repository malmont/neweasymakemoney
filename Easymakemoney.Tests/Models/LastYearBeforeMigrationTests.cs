using Easymakemoney.Models;
using Newtonsoft.Json;
using Xunit;

namespace Easymakemoney.Tests.Models;

/// <summary>IMP-3 : l'an dernier venu de l'archive d'avant la migration est marqué, la marge reste vide (jamais 0).</summary>
public class LastYearBeforeMigrationTests
{
    private const string Json = """
    { "currency": "CAD",
      "lastYear": { "from": "2012-03-01", "to": "2012-03-31", "source": "before_migration" },
      "comparison": { "currentLabel": "1-31 mars 2013", "previousLabel": "29 janv. - 28 févr. 2013", "lastYearLabel": "1-31 mars 2012 (avant la migration)" },
      "indicators": {
        "revenue": { "value": 0, "unit": "money", "previous": 0, "lastYear": 20000, "changeVsPrevious": null, "changeVsLastYear": -100, "changeUnit": "percent" },
        "grossMargin": { "value": 0, "unit": "money", "previous": 0, "lastYear": null, "changeVsLastYear": null }
      },
      "caveats": [ { "code": "LAST_YEAR_BEFORE_MIGRATION", "text": "L'an dernier vient des ventes d'avant la migration, reprises de votre ancienne boutique : chiffre d'affaires et commandes seulement, sans marge ni retours." } ] }
    """;

    [Fact]
    public void BeforeMigration_IsMarkedOnCards_AndInTheComparison()
    {
        var data = JsonConvert.DeserializeObject<UnifiedStatistics>(Json)!;

        Assert.True(data.LastYearBeforeMigration);
        var revenue = data.Cards().Single(c => c.Key == "revenue");
        Assert.Contains("vs avant la migration", revenue.LastYearText);
        Assert.False(data.Cards().Single(c => c.Key == "grossMargin").HasLastYearText);
        Assert.Contains("(avant la migration)", data.ComparisonText);
        Assert.Contains("avant la migration", data.CaveatsText);
    }

    [Fact]
    public void SiteSource_KeepsTheUsualWording()
    {
        var data = JsonConvert.DeserializeObject<UnifiedStatistics>(Json.Replace("before_migration", "site"))!;

        Assert.False(data.LastYearBeforeMigration);
        Assert.Contains("vs an dernier", data.Cards().Single(c => c.Key == "revenue").LastYearText);
    }
}
