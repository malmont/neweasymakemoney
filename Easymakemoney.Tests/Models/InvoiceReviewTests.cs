using System.Globalization;
using Easymakemoney.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Easymakemoney.Tests.Models;

public class InvoiceReviewTests
{
    private static readonly CultureInfo Fr = CultureInfo.GetCultureInfo("fr-FR");

    // Extraction telle que le backend la rend (désérialisée comme dans HttpService : les dates deviennent des dates).
    private const string ExtractionJson = """
    {
      "supplier": { "name": "Mode & Co", "id": null, "candidates": [{ "id": 4, "name": "Mode et Co", "score": 0.7 }], "currency": "USD" },
      "invoice": { "number": "F-2026-118", "date": "2026-11-02", "dueDate": null, "currency": "USD", "exchangeRate": null },
      "amountsMode": "ht", "currency": "CAD",
      "lines": [
        { "n": 1, "label": "Robe lin M rouge", "baseLabel": "Robe lin", "size": "M", "color": "Rouge", "quantity": 3, "unitCost": 1250,
          "total": 3750, "unitCostRead": 1250, "groupKey": "robe-lin", "groupSize": 2, "product": null, "candidates": [] },
        { "n": 2, "label": "Robe lin L bleu", "baseLabel": "Robe lin", "size": "L", "color": "Bleu", "quantity": 2, "unitCost": 1300,
          "total": 2600, "unitCostRead": 1300, "groupKey": "robe-lin", "groupSize": 2, "product": null, "candidates": [] },
        { "n": 3, "label": "Ceinture cuir", "baseLabel": "Ceinture cuir", "size": null, "color": null, "quantity": 5, "unitCost": 800,
          "total": 4000, "unitCostRead": 800, "groupKey": "ceinture-cuir", "groupSize": 1,
          "product": { "id": 51, "name": "Ceinture cuir", "code": "CC1", "score": 0.95, "variantId": 77 }, "candidates": [] }
      ],
      "taxes": [], "shipping": { "label": "Transport", "amount": 1500, "amountRead": 1500 },
      "totals": { "subtotal": 10350, "taxes": 0, "total": 11900 },
      "computed": { "linesTotal": 10350, "shipping": 1500, "taxRate": 0, "total": 11850, "difference": 50 },
      "confidence": { "overall": 0.9, "number": 1, "date": 1, "lines": 0.9, "totals": 0.8 },
      "notes": ["Le total lu diffère de 0,50 du total calculé."],
      "duplicate": null
    }
    """;

    private static InvoiceExtraction Extraction() =>
        InvoiceExtraction.Read(JsonConvert.DeserializeObject<JObject>(ExtractionJson));

    /// <summary>Brouillon entièrement vérifié, fournisseur existant, devise du site.</summary>
    private static InvoiceReviewDraft ValidDraft()
    {
        var draft = InvoiceReview.InitialDraft(Extraction(), Fr);
        draft.Supplier = new InvoiceReviewSupplier { Mode = InvoiceReviewDraft.ModeExisting, Id = 4 };
        foreach (var line in draft.Lines)
            line.Validated = true;
        return draft;
    }

    private static InvoiceConfirmationContext SameCurrency() => new() { SiteCurrency = "CAD", DocumentCurrency = "CAD" };

    private static JObject Body(InvoiceReviewDraft draft, InvoiceConfirmationContext context)
    {
        var result = InvoiceConfirmationBuilder.Build(draft, context);
        Assert.True(result.IsValid, string.Join(" | ", result.Errors));
        return result.Body!;
    }

    // ----- lecture de l'extraction -----

    [Fact]
    public void Read_ParsesSupplierInvoiceLinesAndTotals()
    {
        var extraction = Extraction();

        Assert.Null(extraction.SupplierId);
        Assert.Single(extraction.SupplierCandidates);
        Assert.Equal("F-2026-118", extraction.InvoiceNumber);
        Assert.Equal(new DateTime(2026, 11, 2), extraction.InvoiceDate);
        Assert.Equal("USD", extraction.DocumentCurrency);
        Assert.Equal("CAD", extraction.SiteCurrency);
        Assert.Equal(3, extraction.Lines.Count);
        Assert.Equal(1250, extraction.Lines[0].UnitCost);
        Assert.Equal(77, extraction.Lines[2].Product!.VariantId);
        Assert.Equal(1500, extraction.ShippingAmount);
        Assert.Equal(11900, extraction.TotalRead);
        Assert.Equal(50, extraction.Difference);
        Assert.Single(extraction.Notes);
        Assert.False(extraction.IsDuplicate);
    }

    [Fact]
    public void Read_ToleratesMissingExtraction()
    {
        var extraction = InvoiceExtraction.Read(null);
        Assert.Empty(extraction.Lines);
        Assert.Null(extraction.DocumentCurrency);
    }

    [Fact]
    public void Read_FlagsDuplicate()
    {
        var json = JObject.Parse("""{ "duplicate": { "documentId": 9, "commandeId": 12, "status": "confirmed" }, "lines": [] }""");
        var extraction = InvoiceExtraction.Read(json);
        Assert.True(extraction.IsDuplicate);
        Assert.Equal(12, extraction.DuplicateCommandeId);
    }

    [Fact]
    public void OrderForReview_PutsDoubtfulLinesFirst_KeepingInvoiceOrder()
    {
        var ordered = InvoiceReview.OrderForReview(Extraction().Lines);
        // Lignes 1 et 2 sans correspondance, ligne 3 reconnue avec un score sûr.
        Assert.Equal(new[] { 1, 2, 3 }, ordered.Select(l => l.N));

        var lines = Extraction().Lines;
        lines.Reverse();
        Assert.Equal(new[] { 2, 1, 3 }, InvoiceReview.OrderForReview(lines).Select(l => l.N));
    }

    [Fact]
    public void DoubtReason_LowScoreOrMissingCost()
    {
        var line = new InvoiceExtractedLine { N = 1, UnitCost = 100, Product = new InvoiceProductMatch { Id = 1, Score = 0.6 } };
        Assert.NotNull(InvoiceReview.DoubtReason(line));
        line.Product.Score = 0.9;
        Assert.Null(InvoiceReview.DoubtReason(line));
        line.UnitCost = null;
        Assert.NotNull(InvoiceReview.DoubtReason(line));
    }

    // ----- préremplissage et brouillon -----

    [Fact]
    public void InitialDraft_ExistingOrNewLine_AndSupplierToChoose()
    {
        var draft = InvoiceReview.InitialDraft(Extraction(), Fr);

        Assert.Equal(InvoiceReviewDraft.ModeExisting, draft.Supplier.Mode);
        Assert.Null(draft.Supplier.Id); // proposé, pas présélectionné
        Assert.Equal("2026-11-02", draft.Invoice.Date);
        Assert.True(draft.Shipping.Include);
        Assert.Equal("15,00", draft.Shipping.Amount);

        var newLine = draft.Lines[0];
        Assert.Equal(InvoiceReviewDraft.ModeNew, newLine.Mode);
        Assert.Equal("Robe lin", newLine.Name);
        Assert.Equal("robe-lin", newLine.GroupKey);
        Assert.Equal("12,50", newLine.UnitCost);
        Assert.False(newLine.Validated);

        var existing = draft.Lines[2];
        Assert.True(existing.IsExisting);
        Assert.Equal(51, existing.ProductId);
        Assert.Equal(77, existing.VariantId);
    }

    [Fact]
    public void InitialDraft_NoCandidate_CreatesSupplier()
    {
        var json = JObject.Parse("""{ "supplier": { "name": "Nouveau", "id": null, "candidates": [] }, "lines": [] }""");
        var draft = InvoiceReview.InitialDraft(InvoiceExtraction.Read(json));
        Assert.Equal(InvoiceReviewDraft.ModeNew, draft.Supplier.Mode);
        Assert.Equal("Nouveau", draft.Supplier.Name);
    }

    [Fact]
    public void Draft_RoundTripsThroughJson_AndMergesByLineNumber()
    {
        var initial = InvoiceReview.InitialDraft(Extraction(), Fr);
        var saved = InvoiceReview.InitialDraft(Extraction(), Fr);
        saved.Supplier = new InvoiceReviewSupplier { Mode = InvoiceReviewDraft.ModeExisting, Id = 4 };
        saved.Lines[1].UnitCost = "13,10";
        saved.Lines[1].Validated = true;
        saved.Lines.RemoveAt(2); // ligne absente du brouillon : garde la lecture

        // Comme à la lecture du document : Newtonsoft transforme les dates, FromJson doit les rendre en texte.
        var token = JsonConvert.DeserializeObject<JToken>(saved.ToJson().ToString(Formatting.None));
        var restored = InvoiceReviewDraft.FromJson(token);
        var merged = InvoiceReview.Merge(initial, restored);

        Assert.Equal(4, merged.Supplier.Id);
        Assert.Equal("2026-11-02", merged.Invoice.Date);
        Assert.Equal(3, merged.Lines.Count);
        Assert.Equal("13,10", merged.Lines[1].UnitCost);
        Assert.True(merged.Lines[1].Validated);
        Assert.Equal(51, merged.Lines[2].ProductId);
        Assert.False(merged.Lines[2].Validated);
    }

    [Fact]
    public void FromJson_IgnoresUnknownShapes()
    {
        Assert.Null(InvoiceReviewDraft.FromJson(null));
        Assert.Null(InvoiceReviewDraft.FromJson(new JArray()));
        Assert.Null(InvoiceReviewDraft.FromJson(JObject.Parse("""{ "foo": 1 }""")));
    }

    // ----- saisies -----

    [Theory]
    [InlineData("12,50", 1250L)]
    [InlineData("12.50", 1250L)]
    [InlineData(" 1 234,5 ", 123450L)]
    [InlineData("0", 0L)]
    public void ParseCents_ReadsCommaAndPoint(string text, long expected) =>
        Assert.Equal(expected, InvoiceReview.ParseCents(text));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("-3")]
    public void ParseCents_RejectsInvalid(string? text) => Assert.Null(InvoiceReview.ParseCents(text));

    [Theory]
    [InlineData("3", 3)]
    [InlineData("0", null)]
    [InlineData("2,5", null)]
    [InlineData("x", null)]
    public void ParseQuantity_WholeNumberFromOne(string text, int? expected) =>
        Assert.Equal(expected, InvoiceReview.ParseQuantity(text));

    [Fact]
    public void SalePrice_CostTimesCoefficientThenRate()
    {
        Assert.Equal(2500, InvoiceReview.SalePriceCents(1250, 2));
        Assert.Equal(3375, InvoiceReview.SalePriceCents(1250, 2, 1.35));
    }

    // ----- corps de « confirmer » -----

    [Fact]
    public void Build_ExistingAndNewLines()
    {
        var body = Body(ValidDraft(), SameCurrency());

        Assert.Equal(4, body["supplier"]!["id"]!.Value<int>());
        Assert.Equal("F-2026-118", body["invoice"]!["number"]!.Value<string>());
        Assert.Equal("2026-11-02", body["invoice"]!["date"]!.Value<string>());
        Assert.Null(body["invoice"]!["exchangeRate"]);

        var lines = (JArray)body["lines"]!;
        Assert.Equal(3, lines.Count);

        var created = lines[0];
        Assert.Equal(1, created["n"]!.Value<int>());
        Assert.Equal(3, created["quantity"]!.Value<int>());
        Assert.Equal(1250, created["unitCost"]!.Value<long>());
        Assert.Null(created["productId"]);
        Assert.Equal("Robe lin", created["create"]!["name"]!.Value<string>());
        Assert.Equal(2.0, created["create"]!["coefficient"]!.Value<double>());
        Assert.Equal("M", created["size"]!.Value<string>());
        Assert.Equal("Rouge", created["color"]!.Value<string>());

        var attached = lines[2];
        Assert.Equal(51, attached["productId"]!.Value<int>());
        Assert.Equal(77, attached["variantId"]!.Value<int>());
        Assert.Null(attached["create"]);
        Assert.Null(attached["size"]);
    }

    [Fact]
    public void Build_GroupKey_SharesFirstLineNameAndCoefficient()
    {
        var draft = ValidDraft();
        draft.Lines[0].Name = "Robe en lin";
        draft.Lines[0].Coefficient = "2,5";
        draft.Lines[1].Name = "Autre nom";
        draft.Lines[1].Coefficient = "3";
        // La ceinture devient un nouvel article : son propre groupe.
        draft.Lines[2].Mode = InvoiceReviewDraft.ModeNew;
        draft.Lines[2].Name = "Ceinture";

        var lines = (JArray)Body(draft, SameCurrency())["lines"]!;

        Assert.Equal("Robe en lin", lines[0]["create"]!["name"]!.Value<string>());
        Assert.Equal("Robe en lin", lines[1]["create"]!["name"]!.Value<string>());
        Assert.Equal(2.5, lines[1]["create"]!["coefficient"]!.Value<double>());
        Assert.Equal("robe-lin", lines[0]["create"]!["groupKey"]!.Value<string>());
        Assert.Equal("robe-lin", lines[1]["create"]!["groupKey"]!.Value<string>());
        Assert.Equal("Ceinture", lines[2]["create"]!["name"]!.Value<string>());
        Assert.Equal("ceinture-cuir", lines[2]["create"]!["groupKey"]!.Value<string>());
        Assert.Null(lines[2]["productId"]);
        Assert.Null(lines[2]["variantId"]);
    }

    [Fact]
    public void Build_LinesFollowInvoiceOrder()
    {
        var draft = ValidDraft();
        draft.Lines.Reverse();
        var lines = (JArray)Body(draft, SameCurrency())["lines"]!;
        Assert.Equal(new[] { 1, 2, 3 }, lines.Select(l => l["n"]!.Value<int>()));
    }

    [Fact]
    public void Build_CorrectedAmountsAreReadAsCents()
    {
        var draft = ValidDraft();
        draft.Lines[0].UnitCost = "12,5";
        draft.Lines[1].UnitCost = "9.99";
        var lines = (JArray)Body(draft, SameCurrency())["lines"]!;
        Assert.Equal(1250, lines[0]["unitCost"]!.Value<long>());
        Assert.Equal(999, lines[1]["unitCost"]!.Value<long>());
    }

    [Fact]
    public void Build_ForeignCurrency_RequiresRate()
    {
        var context = new InvoiceConfirmationContext { SiteCurrency = "CAD", DocumentCurrency = "USD" };
        var draft = ValidDraft();

        var missing = InvoiceConfirmationBuilder.Build(draft, context);
        Assert.False(missing.IsValid);
        Assert.Contains(missing.Errors, e => e.Contains("taux de change") && e.Contains("USD"));

        draft.Invoice.ExchangeRate = "1,35";
        var body = Body(draft, context);
        Assert.Equal(1.35, body["invoice"]!["exchangeRate"]!.Value<double>());
    }

    [Fact]
    public void Build_ForeignCurrency_NewSupplierTakesInvoiceCurrency()
    {
        var draft = ValidDraft();
        draft.Supplier = new InvoiceReviewSupplier { Mode = InvoiceReviewDraft.ModeNew, Name = " Mode & Co ", TypeFournisseurId = 2 };
        draft.Invoice.ExchangeRate = "1.4";

        var body = Body(draft, new InvoiceConfirmationContext { SiteCurrency = "CAD", DocumentCurrency = "usd" });

        var create = body["supplier"]!["create"]!;
        Assert.Equal("Mode & Co", create["name"]!.Value<string>());
        Assert.Equal(2, create["typeFournisseurId"]!.Value<int>());
        Assert.Equal("USD", create["currency"]!.Value<string>());
        Assert.Null(body["supplier"]!["id"]);
    }

    [Fact]
    public void Build_SiteCurrency_IgnoresRateAndSupplierCurrency()
    {
        var draft = ValidDraft();
        draft.Invoice.ExchangeRate = "1,35";
        draft.Supplier = new InvoiceReviewSupplier { Mode = InvoiceReviewDraft.ModeNew, Name = "X", TypeFournisseurId = 1 };

        var body = Body(draft, SameCurrency());

        Assert.Null(body["invoice"]!["exchangeRate"]);
        Assert.Null(body["supplier"]!["create"]!["currency"]);
    }

    [Fact]
    public void Build_ExistingSupplierInForeignCurrency_RequiresRate()
    {
        var context = new InvoiceConfirmationContext { SiteCurrency = "CAD", DocumentCurrency = "CAD", SupplierCurrency = "EUR" };
        Assert.Equal("EUR", InvoiceConfirmationBuilder.ForeignCurrency(ValidDraft(), context));
        Assert.False(InvoiceConfirmationBuilder.Build(ValidDraft(), context).IsValid);
    }

    [Fact]
    public void Build_Shipping_CheckedOrNot()
    {
        var draft = ValidDraft();
        Assert.Equal(1500, Body(draft, SameCurrency())["shipping"]!["amount"]!.Value<long>());

        draft.Shipping.Amount = "17,25";
        Assert.Equal(1725, Body(draft, SameCurrency())["shipping"]!["amount"]!.Value<long>());

        draft.Shipping.Include = false;
        Assert.Null(Body(draft, SameCurrency())["shipping"]);

        draft.Shipping.Include = true;
        draft.Shipping.Amount = "abc";
        Assert.False(InvoiceConfirmationBuilder.Build(draft, SameCurrency()).IsValid);
    }

    [Fact]
    public void Build_ClassificationDateAndForce()
    {
        var plain = Body(ValidDraft(), SameCurrency());
        Assert.Null(plain["classificationDate"]);
        Assert.Null(plain["force"]);

        var context = SameCurrency();
        context.ClassificationDate = "2026-11-09";
        context.Force = true;
        var body = Body(ValidDraft(), context);
        Assert.Equal("2026-11-09", body["classificationDate"]!.Value<string>());
        Assert.True(body["force"]!.Value<bool>());
        // La date de la facture reste celle du document.
        Assert.Equal("2026-11-02", body["invoice"]!["date"]!.Value<string>());
    }

    [Fact]
    public void Build_RefusesUnvalidatedLineOrMissingSupplier()
    {
        var draft = ValidDraft();
        draft.Lines[1].Validated = false;
        draft.Supplier = new InvoiceReviewSupplier { Mode = InvoiceReviewDraft.ModeExisting, Id = null };

        var result = InvoiceConfirmationBuilder.Build(draft, SameCurrency());

        Assert.False(result.IsValid);
        Assert.Null(result.Body);
        Assert.Contains(result.Errors, e => e.Contains("fournisseur"));
        Assert.Contains(result.Errors, e => e.Contains("à valider"));
    }

    [Fact]
    public void Build_NewSupplierNeedsNameAndType()
    {
        var draft = ValidDraft();
        draft.Supplier = new InvoiceReviewSupplier { Mode = InvoiceReviewDraft.ModeNew, Name = " " };
        var result = InvoiceConfirmationBuilder.Build(draft, SameCurrency());
        Assert.Equal(2, result.Errors.Count(e => e.Contains("nouveau fournisseur")));
    }

    [Fact]
    public void LineProblem_ChecksQuantityCostAndLink()
    {
        var line = new InvoiceReviewLine { N = 1, Quantity = "2", UnitCost = "5", Mode = InvoiceReviewDraft.ModeNew, Name = "Pull" };
        Assert.Null(InvoiceReview.LineProblem(line));

        line.Name = "";
        Assert.NotNull(InvoiceReview.LineProblem(line));

        line.Mode = InvoiceReviewDraft.ModeExisting;
        Assert.NotNull(InvoiceReview.LineProblem(line));
        line.ProductId = 8;
        Assert.Null(InvoiceReview.LineProblem(line));

        line.Quantity = "0";
        Assert.NotNull(InvoiceReview.LineProblem(line));
    }
}
