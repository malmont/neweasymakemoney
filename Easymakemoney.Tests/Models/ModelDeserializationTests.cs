using Easymakemoney.Models;
using Newtonsoft.Json;
using Xunit;

namespace Easymakemoney.Tests.Models;

public class ModelDeserializationTests
{
    [Fact]
    public void ListProduct_Deserialization_HandlesNullsAndDecimalMultiplier()
    {
        var json = """
        {
            "id": 101,
            "name": "Robe Bohème",
            "description": "Robe longue",
            "purchasePrice": null,
            "coefficientMultiplier": 2.5,
            "quantity": null,
            "freezeQuantity": null,
            "price": null,
            "isbestseller": null,
            "isnewarrival": null,
            "isfeatured": null,
            "isspecialoffer": null
        }
        """;

        var product = JsonConvert.DeserializeObject<ListProduct>(json);

        Assert.NotNull(product);
        Assert.Equal(101, product.id);
        Assert.Equal("Robe Bohème", product.name);
        Assert.Null(product.purchasePrice);
        Assert.Equal(2.5, product.coefficientMultiplier);
        Assert.Null(product.quantity);
        Assert.Null(product.price);
        Assert.Null(product.isbestseller);
        Assert.Null(product.isnewarrival);
        Assert.Null(product.isfeatured);
        Assert.Null(product.isspecialoffer);
    }

    [Fact]
    public void MonthlyRevenueData_Deserialization_NumericMonth_TranslatesData1ToFrench()
    {
        var json = """{ "month": 4, "revenue": 150000.0 }""";

        var data = JsonConvert.DeserializeObject<MonthlyRevenueData>(json);

        Assert.NotNull(data);
        Assert.Equal("4", data.Month);
        Assert.Equal("avril", data.Data1);
        Assert.Equal(150000.0, data.Data2);
    }

    [Fact]
    public void MonthlyOrderData_Deserialization_NumericMonth_TranslatesData1ToFrench()
    {
        var json = """{ "month": 6, "orderCount": 42 }""";

        var data = JsonConvert.DeserializeObject<MonthlyOrderData>(json);

        Assert.NotNull(data);
        Assert.Equal("6", data.Month);
        Assert.Equal("juin", data.Data1);
        Assert.Equal(42.0, data.Data2);
    }

    [Fact]
    public void MonthlyAverageOrderValueData_Deserialization_NumericMonth_TranslatesData1ToFrench()
    {
        var json = """{ "month": 9, "averageOrderValue": 3550.0 }""";

        var data = JsonConvert.DeserializeObject<MonthlyAverageOrderValueData>(json);

        Assert.NotNull(data);
        Assert.Equal("9", data.Month);
        Assert.Equal("septembre", data.Data1);
        Assert.Equal(3550.0, data.Data2);
    }

    [Fact]
    public void MonthlyTaxData_Deserialization_NumericMonth_TranslatesData1ToFrench()
    {
        var json = """{ "month": 12, "total_tax": 8900.0 }""";

        var data = JsonConvert.DeserializeObject<MonthlyTaxData>(json);

        Assert.NotNull(data);
        Assert.Equal("12", data.Month);
        Assert.Equal("décembre", data.Data1);
        Assert.Equal(8900.0, data.Data2);
    }

    [Fact]
    public void MonthlyCarrierData_Deserialization_NumericMonth_TranslatesData1ToFrench()
    {
        var json = """{ "month": 1, "carrier_count": 12500.0 }""";

        var data = JsonConvert.DeserializeObject<MonthlyCarrierData>(json);

        Assert.NotNull(data);
        Assert.Equal("1", data.Month);
        Assert.Equal("janvier", data.Data1);
        Assert.Equal(12500.0, data.Data2);
    }

    [Fact]
    public void MonthlyFraisData_Deserialization_NumericMonth_TranslatesPeriodLabelToFrench()
    {
        var json = """
        {
            "month": 2,
            "total_frais": {
                "frais_reel": 2500.0,
                "frais_fixe": 1000.0,
                "total": 3500.0
            }
        }
        """;

        var data = JsonConvert.DeserializeObject<FraisEvolution.MonthlyFraisData>(json);

        Assert.NotNull(data);
        Assert.Equal("2", data.Month);
        Assert.Equal("février", data.PeriodLabel);
        Assert.NotNull(data.TotalFrais);
    }

    [Fact]
    public void Models_WithLongCentsAmounts_DeserializeProperly()
    {
        var colJson = """{ "budgetCollection": 150000, "nomCollection": "Automne" }""";
        var col = JsonConvert.DeserializeObject<ListCollection>(colJson);
        Assert.NotNull(col);
        Assert.Equal(150000L, col.budgetCollection);

        var cmdJson = """{ "budget": 85000, "name": "Commande Tissu" }""";
        var cmd = JsonConvert.DeserializeObject<ListCommand>(cmdJson);
        Assert.NotNull(cmd);
        Assert.Equal(85000L, cmd.budget);

        var fdpJson = """{ "price": 1250, "name": "Colissimo" }""";
        var fdp = JsonConvert.DeserializeObject<FraisDePort>(fdpJson);
        Assert.NotNull(fdp);
        Assert.Equal(1250L, fdp.price);

        var ndfJson = """{ "montant": 4500, "name": "Essence" }""";
        var ndf = JsonConvert.DeserializeObject<NoteDeFrais>(ndfJson);
        Assert.NotNull(ndf);
        Assert.Equal(4500L, ndf.montant);
    }

    [Fact]
    public void Models_WithNullCentsAmounts_DeserializeAsNull()
    {
        var col = JsonConvert.DeserializeObject<ListCollection>("""{ "budgetCollection": null }""");
        Assert.NotNull(col);
        Assert.Null(col.budgetCollection);

        var cmd = JsonConvert.DeserializeObject<ListCommand>("""{ "budget": null }""");
        Assert.NotNull(cmd);
        Assert.Null(cmd.budget);

        var fdp = JsonConvert.DeserializeObject<FraisDePort>("""{ "price": null }""");
        Assert.NotNull(fdp);
        Assert.Null(fdp.price);

        var ndf = JsonConvert.DeserializeObject<NoteDeFrais>("""{ "montant": null }""");
        Assert.NotNull(ndf);
        Assert.Null(ndf.montant);
    }
}

