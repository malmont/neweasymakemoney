using Easymakemoney.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Easymakemoney.Tests.Models;

public class GestionDocumentTests
{
    [Fact]
    public void GestionDocument_DeserializesDoneStatus_WithFullExtractionObject()
    {
        var json = """
        {
            "id": 10,
            "type": "facture",
            "status": "done",
            "error": null,
            "extraction": {
                "supplier": "Fournisseur Test",
                "invoiceNumber": "FA-2026-001",
                "totalCents": 12500,
                "lines": [
                    { "name": "Produit A", "quantity": 5, "unitCostCents": 2500 }
                ]
            }
        }
        """;

        var doc = JsonConvert.DeserializeObject<GestionDocument>(json);

        Assert.NotNull(doc);
        Assert.Equal(10, doc.id);
        Assert.Equal("facture", doc.type);
        Assert.Equal("done", doc.status);
        Assert.Null(doc.error);

        Assert.NotNull(doc.extraction);
        Assert.IsType<JObject>(doc.extraction);
        Assert.Equal("Fournisseur Test", (string?)doc.extraction["supplier"]);
        Assert.Equal("FA-2026-001", (string?)doc.extraction["invoiceNumber"]);
        Assert.Equal(12500L, (long?)doc.extraction["totalCents"]);

        var lines = doc.extraction["lines"] as JArray;
        Assert.NotNull(lines);
        Assert.Single(lines);
        Assert.Equal("Produit A", (string?)lines[0]["name"]);
        Assert.Equal(5, (int?)lines[0]["quantity"]);
        Assert.Equal(2500L, (long?)lines[0]["unitCostCents"]);
    }

    [Fact]
    public void GestionDocument_DeserializesFailedStatus_WithError()
    {
        var json = """
        {
            "id": 11,
            "type": "recu",
            "status": "failed",
            "error": "Document illisible ou corrompu",
            "extraction": null
        }
        """;

        var doc = JsonConvert.DeserializeObject<GestionDocument>(json);

        Assert.NotNull(doc);
        Assert.Equal(11, doc.id);
        Assert.Equal("recu", doc.type);
        Assert.Equal("failed", doc.status);
        Assert.Equal("Document illisible ou corrompu", doc.error);
        Assert.Null(doc.extraction);
    }

    [Fact]
    public void GestionDocument_DeserializesProcessingStatus()
    {
        var json = """
        {
            "id": 12,
            "type": "transport",
            "status": "processing",
            "error": null,
            "extraction": null
        }
        """;

        var doc = JsonConvert.DeserializeObject<GestionDocument>(json);

        Assert.NotNull(doc);
        Assert.Equal(12, doc.id);
        Assert.Equal("transport", doc.type);
        Assert.Equal("processing", doc.status);
        Assert.Null(doc.error);
        Assert.Null(doc.extraction);
    }

    [Fact]
    public void GestionDocument_EmptyOrNullExtraction_HandledGracefully()
    {
        var jsonWithoutExtraction = """{ "id": 13, "status": "done" }""";
        var docWithout = JsonConvert.DeserializeObject<GestionDocument>(jsonWithoutExtraction);

        Assert.NotNull(docWithout);
        Assert.Equal(13, docWithout.id);
        Assert.Equal("done", docWithout.status);
        Assert.Null(docWithout.extraction);

        var jsonWithEmptyExtraction = """{ "id": 14, "status": "done", "extraction": {} }""";
        var docWithEmpty = JsonConvert.DeserializeObject<GestionDocument>(jsonWithEmptyExtraction);

        Assert.NotNull(docWithEmpty);
        Assert.Equal(14, docWithEmpty.id);
        Assert.NotNull(docWithEmpty.extraction);
        Assert.Empty(docWithEmpty.extraction);
    }
}

