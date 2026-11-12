using System.Globalization;
using Easymakemoney.Models;
using Easymakemoney.Tests.Fakes;
using Easymakemoney.UseCase.DocumentUseCase;
using Easymakemoney.Utilities;
using Easymakemoney.ViewModels.Lists;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Easymakemoney.Tests.ViewModels;

/// <summary>Lot backend APERÇU-FACT : prix de vente prévu, marge et total des lignes viennent du serveur ; estimation locale seulement sans la route.</summary>
[Collection("SiteCurrency")]
public class InvoicePreviewTests : IDisposable
{
    private readonly CultureInfo _originalCulture = CultureInfo.CurrentCulture;

    public InvoicePreviewTests()
    {
        CultureInfo.CurrentCulture = new CultureInfo("fr-CA");
        SiteCurrency.Reset();
        SiteCurrency.SetCurrency("CAD");
    }

    public void Dispose()
    {
        CultureInfo.CurrentCulture = _originalCulture;
        SiteCurrency.Reset();
    }

    // Exemple chiffré du backend : facture en EUR, site en CAD, taux 1,48.
    private const string PreviewJson = """
    { "currency": "CAD", "invoiceCurrency": "EUR", "exchangeRate": 1.48, "pricesIncludeTax": false,
      "lines": [
        { "n": 1, "quantity": 2, "unitCost": 2000, "total": 4000, "unitCostConverted": 2960, "kind": "create", "coefficient": 2.5, "plannedPrice": 7770, "currentPrice": null, "plannedMargin": 4810, "plannedMarginRate": 61.9, "groupKey": "robe" },
        { "n": 2, "quantity": 1, "unitCost": 999, "total": 999, "unitCostConverted": 1479, "kind": "create", "coefficient": null, "plannedPrice": null, "currentPrice": null, "plannedMargin": null, "plannedMarginRate": null, "groupKey": "ceinture" },
        { "n": 3, "quantity": 1, "unitCost": 500, "total": 500, "unitCostConverted": 740, "kind": "existing", "coefficient": null, "plannedPrice": null, "currentPrice": 2500, "plannedMargin": null, "plannedMarginRate": null, "groupKey": null },
        { "n": 4, "quantity": 1, "unitCost": 100, "total": 100, "unitCostConverted": null, "kind": "unresolved", "coefficient": null, "plannedPrice": null, "currentPrice": null, "plannedMargin": null, "plannedMarginRate": null, "groupKey": null } ],
      "totals": { "lines": 5599, "shipping": 0, "taxRate": 20, "taxes": 1120, "total": 6719, "read": { "subtotal": 5599, "taxes": 1120, "total": 6719 }, "difference": 0 },
      "issues": [ { "path": "lines[2].unitCost", "code": "MUST_BE_NUMBER" } ] }
    """;

    private static InvoicePreview Parse() => JsonConvert.DeserializeObject<InvoicePreview>(PreviewJson)!;

    [Fact]
    public void PriceTexts_FromServer()
    {
        var preview = Parse();

        var created = preview.PriceText(preview.Line(1)!);
        Assert.StartsWith("Prix de vente : ", created);
        Assert.Contains("marge", created);
        Assert.Contains("61,9 %", created);
        Assert.Equal("Prix de vente : à compléter (voir la ligne)", preview.PriceText(preview.Line(2)!));
        Assert.StartsWith("Prix de vente : inchangé (", preview.PriceText(preview.Line(3)!));
        Assert.Equal("Prix de vente : article à choisir", preview.PriceText(preview.Line(4)!));
        Assert.NotNull(preview.LinesTotalText);
        Assert.Null(preview.Line(9));
    }

    [Fact]
    public void PriceText_TaxIncludedSite_SaysSo()
    {
        var preview = Parse();
        preview.pricesIncludeTax = true;

        Assert.Contains("taxes comprises", preview.PriceText(preview.Line(1)!));
    }

    private static GestionDocument Document() => new()
    {
        id = 42,
        type = "facture_fournisseur",
        status = "extracted",
        extraction = JObject.Parse("""
        { "supplier": { "name": "Mode & Co", "id": 4, "candidates": [ { "id": 4, "name": "Mode & Co", "score": 0.9 } ] },
          "invoice": { "number": "F-1", "date": "2026-11-02", "currency": "CAD" }, "currency": "CAD", "amountsMode": "ht",
          "lines": [ { "n": 1, "label": "Robe lin M", "quantity": 3, "unitCost": 1250, "groupKey": "robe-lin", "product": { "id": 10, "variantId": 20, "name": "Robe lin" } } ],
          "totals": { "total": 3750 }, "computed": { "total": 3750, "difference": 0 } }
        """),
    };

    private static (InvoiceReviewViewModel Vm, FakeDocumentReaderService Docs) Create()
    {
        var docs = new FakeDocumentReaderService { DocumentResult = Document() };
        var suppliers = new FakeListFournisseursServices();
        suppliers.Fournisseurs.Add(new ListFournisseur { id = 4, name = "Mode & Co", purchaseCurrency = "CAD" });
        suppliers.Types.Add(new TypeFournisseur { id = 1, name = "Grossiste" });
        var vm = new InvoiceReviewViewModel(new DocumentReaderUseCase(docs), suppliers, new FakeCommandeReceptionService(), new FakeDialogService());
        return (vm, docs);
    }

    [Fact]
    public async Task RouteAvailable_LinesAndTotalComeFromServer()
    {
        var (vm, docs) = Create();
        docs.PreviewResult = new InvoicePreview
        {
            currency = "CAD", invoiceCurrency = "CAD",
            lines = { new InvoicePreviewLine { n = 1, kind = "existing", currentPrice = 4900 } },
            totals = new InvoicePreviewTotals { lines = 3750 },
        };

        await vm.InitAsync(42);

        Assert.Single(docs.PreviewCalls);
        Assert.Equal(42, docs.PreviewCalls[0].Id);
        Assert.StartsWith("Prix de vente : inchangé (", vm.Lines.Single().SalePriceText);
        Assert.Equal(docs.PreviewResult.LinesTotalText, vm.LinesTotalText);
    }

    [Fact]
    public async Task RouteMissing_KeepsLocalEstimate_AndStopsAsking()
    {
        var (vm, docs) = Create(); // le faux répond 404 par défaut

        await vm.InitAsync(42);
        await vm.RefreshPreviewAsync();

        Assert.Single(docs.PreviewCalls);
        Assert.Equal("Prix de vente : inchangé", vm.Lines.Single().SalePriceText);
        Assert.NotEqual("À compléter", vm.LinesTotalText);
    }
}
