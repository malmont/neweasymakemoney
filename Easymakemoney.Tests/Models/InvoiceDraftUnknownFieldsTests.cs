using Easymakemoney.Models;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Easymakemoney.Tests.Models;

/// <summary>
/// Brouillon de facture partagé avec le web : les champs que le mobile ne connaît pas sont gardés à la réécriture
/// (défaut signalé par Gemini en préparant le lot 4l).
/// </summary>
public class InvoiceDraftUnknownFieldsTests
{
    private static JObject Original() => JObject.Parse("""
    { "version": 1, "webOnly": { "openedTab": "lines" },
      "supplier": { "mode": "existing", "id": 1, "note": "garder" },
      "invoice": { "number": "F-1", "date": "2026-11-12", "dueDate": "2026-12-12" },
      "shipping": { "include": false, "amount": null, "carrierHint": "UPS" },
      "lines": [ { "n": 1, "validated": true, "mode": "existing", "productId": 44, "quantity": "2", "unitCost": "5", "photoAsked": true },
                 { "n": 2, "validated": false, "mode": "new", "name": "Nouveau", "quantity": "3", "unitCost": "4" } ] }
    """);

    [Fact]
    public void Rewrite_KeepsUnknownFieldsEverywhere()
    {
        var built = InvoiceReviewDraft.FromJson(Original())!.ToJson();
        built["invoice"]!["number"] = "F-1 corrigée";

        var merged = InvoiceReviewDraft.KeepUnknownFields(built, Original());

        Assert.Equal("lines", merged["webOnly"]!["openedTab"]!.Value<string>());
        Assert.Equal("garder", merged["supplier"]!["note"]!.Value<string>());
        Assert.Equal("2026-12-12", merged["invoice"]!["dueDate"]!.Value<string>());
        Assert.Equal("UPS", merged["shipping"]!["carrierHint"]!.Value<string>());
        Assert.True(merged["lines"]![0]!["photoAsked"]!.Value<bool>());
        Assert.Null(merged["lines"]![1]!["photoAsked"]);
        // Les champs connus restent ceux du mobile
        Assert.Equal("F-1 corrigée", merged["invoice"]!["number"]!.Value<string>());
    }

    [Fact]
    public void KnownFieldsAreNeverOverwrittenByOriginal()
    {
        var built = InvoiceReviewDraft.FromJson(Original())!.ToJson();
        built["lines"]![0]!["quantity"] = "9";

        var merged = InvoiceReviewDraft.KeepUnknownFields(built, Original());

        Assert.Equal("9", merged["lines"]![0]!["quantity"]!.Value<string>());
    }

    [Fact]
    public void NoOriginal_ReturnsBuiltAsIs()
    {
        var built = new InvoiceReviewDraft().ToJson();

        Assert.Same(built, InvoiceReviewDraft.KeepUnknownFields(built, null));
    }

    [Fact]
    public void RemovedLine_DoesNotComeBack()
    {
        var built = InvoiceReviewDraft.FromJson(Original())!.ToJson();
        ((JArray)built["lines"]!).RemoveAt(1);

        var merged = InvoiceReviewDraft.KeepUnknownFields(built, Original());

        Assert.Single((JArray)merged["lines"]!);
    }
}
