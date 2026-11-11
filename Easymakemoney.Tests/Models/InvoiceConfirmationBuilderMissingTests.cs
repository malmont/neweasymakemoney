using System.Globalization;
using Easymakemoney.Models;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Easymakemoney.Tests.Models;

public class InvoiceConfirmationBuilderMissingTests
{
    private static readonly CultureInfo Fr = CultureInfo.GetCultureInfo("fr-FR");

    private static InvoiceConfirmationContext SameCurrency() => new()
    {
        SiteCurrency = "CAD",
        DocumentCurrency = "CAD"
    };

    private static InvoiceReviewDraft MakeValidDraft()
    {
        return new InvoiceReviewDraft
        {
            Supplier = new InvoiceReviewSupplier
            {
                Mode = InvoiceReviewDraft.ModeExisting,
                Id = 4,
                Name = "Fournisseur A"
            },
            Invoice = new InvoiceReviewInvoice
            {
                Number = "FA-2026-01",
                Date = "2026-11-02",
                ExchangeRate = null
            },
            Shipping = new InvoiceReviewShipping
            {
                Include = false,
                Amount = "15,00"
            },
            Lines = new List<InvoiceReviewLine>
            {
                new()
                {
                    N = 1,
                    Label = "Robe lin",
                    Name = "Robe lin",
                    GroupKey = "robe-lin",
                    Quantity = "3",
                    UnitCost = "12,50",
                    Coefficient = "2,0",
                    Mode = InvoiceReviewDraft.ModeNew,
                    Validated = true
                },
                new()
                {
                    N = 2,
                    Label = "Ceinture cuir",
                    ProductId = 51,
                    VariantId = 77,
                    Quantity = "5",
                    UnitCost = "8,00",
                    Mode = InvoiceReviewDraft.ModeExisting,
                    Validated = true
                }
            }
        };
    }

    [Fact]
    public void Build_NoLines_ReturnsError()
    {
        var draft = MakeValidDraft();
        draft.Lines.Clear();

        var result = InvoiceConfirmationBuilder.Build(draft, SameCurrency());

        Assert.False(result.IsValid);
        Assert.Null(result.Body);
        Assert.Contains(result.Errors, e => e.Contains("La facture n'a aucune ligne."));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("date-invalide")]
    [InlineData("2026/11/02")]
    public void Build_MissingOrInvalidInvoiceDate_ReturnsError(string? invalidDate)
    {
        var draft = MakeValidDraft();
        draft.Invoice.Date = invalidDate;

        var result = InvoiceConfirmationBuilder.Build(draft, SameCurrency());

        Assert.False(result.IsValid);
        Assert.Null(result.Body);
        Assert.Contains(result.Errors, e => e.Contains("Indiquez la date de la facture."));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Build_ExistingSupplierWithInvalidId_ReturnsError(int invalidId)
    {
        var draft = MakeValidDraft();
        draft.Supplier = new InvoiceReviewSupplier
        {
            Mode = InvoiceReviewDraft.ModeExisting,
            Id = invalidId
        };

        var result = InvoiceConfirmationBuilder.Build(draft, SameCurrency());

        Assert.False(result.IsValid);
        Assert.Null(result.Body);
        Assert.Contains(result.Errors, e => e.Contains("Choisissez le fournisseur."));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(-5)]
    public void Build_ExistingLineWithoutVariant_OmitsVariantId(int? variantId)
    {
        var draft = MakeValidDraft();
        draft.Lines[1].VariantId = variantId;

        var result = InvoiceConfirmationBuilder.Build(draft, SameCurrency());

        Assert.True(result.IsValid, string.Join(" | ", result.Errors));
        Assert.NotNull(result.Body);

        var lines = (JArray)result.Body!["lines"]!;
        var existingLine = lines[1];
        Assert.Equal(51, existingLine["productId"]!.Value<int>());
        Assert.Null(existingLine["variantId"]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Build_NewLineWithoutGroupKey_OmitsGroupKey(string? emptyGroupKey)
    {
        var draft = MakeValidDraft();
        draft.Lines[0].GroupKey = emptyGroupKey;

        var result = InvoiceConfirmationBuilder.Build(draft, SameCurrency());

        Assert.True(result.IsValid, string.Join(" | ", result.Errors));
        Assert.NotNull(result.Body);

        var lines = (JArray)result.Body!["lines"]!;
        var newLine = lines[0];
        Assert.NotNull(newLine["create"]);
        Assert.Null(newLine["create"]!["groupKey"]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Build_NewLineWithoutCoefficient_OmitsCoefficient(string? emptyCoeff)
    {
        var draft = MakeValidDraft();
        draft.Lines[0].Coefficient = emptyCoeff;

        var result = InvoiceConfirmationBuilder.Build(draft, SameCurrency());

        Assert.True(result.IsValid, string.Join(" | ", result.Errors));
        Assert.NotNull(result.Body);

        var lines = (JArray)result.Body!["lines"]!;
        var newLine = lines[0];
        Assert.NotNull(newLine["create"]);
        Assert.Null(newLine["create"]!["coefficient"]);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-2")]
    public void Build_NewLineWithInvalidNegativeOrZeroCoefficient_ReportsError(string invalidCoeff)
    {
        var draft = MakeValidDraft();
        draft.Lines[0].Coefficient = invalidCoeff;

        var result = InvoiceConfirmationBuilder.Build(draft, SameCurrency());

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Coefficient invalide"));
    }

    [Fact]
    public void Build_ValidatedLineWithZeroQuantityOrInvalidCost_ReportsError()
    {
        // 1. Quantité à 0
        var draftQty = MakeValidDraft();
        draftQty.Lines[0].Quantity = "0";

        var resultQty = InvoiceConfirmationBuilder.Build(draftQty, SameCurrency());
        Assert.False(resultQty.IsValid);
        Assert.Contains(resultQty.Errors, e => e.Contains("Quantité invalide"));

        // 2. Coût non numérique
        var draftCost = MakeValidDraft();
        draftCost.Lines[0].UnitCost = "invalide";

        var resultCost = InvoiceConfirmationBuilder.Build(draftCost, SameCurrency());
        Assert.False(resultCost.IsValid);
        Assert.Contains(resultCost.Errors, e => e.Contains("Coût hors taxes invalide"));
    }

    [Fact]
    public void Build_ShippingIncludedWithZeroAmount_OmitsShipping()
    {
        var draft = MakeValidDraft();
        draft.Shipping.Include = true;
        draft.Shipping.Amount = "0";

        var result = InvoiceConfirmationBuilder.Build(draft, SameCurrency());

        Assert.True(result.IsValid, string.Join(" | ", result.Errors));
        Assert.NotNull(result.Body);
        Assert.Null(result.Body!["shipping"]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Build_InvoiceNumberOptional_OmitsNumberWhenBlank(string? emptyNumber)
    {
        var draft = MakeValidDraft();
        draft.Invoice.Number = emptyNumber;

        var result = InvoiceConfirmationBuilder.Build(draft, SameCurrency());

        Assert.True(result.IsValid, string.Join(" | ", result.Errors));
        Assert.NotNull(result.Body);
        Assert.Null(result.Body!["invoice"]!["number"]);
    }
}
