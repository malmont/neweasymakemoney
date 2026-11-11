using System.Globalization;
using Easymakemoney.Models;
using Easymakemoney.Utilities;
using Easymakemoney.ViewModels.Lists;
using Xunit;

namespace Easymakemoney.Tests.ViewModels;

[Collection("SiteCurrency")]
public class InvoiceLineItemViewModelTests : IDisposable
{
    private readonly CultureInfo _originalCulture;

    public InvoiceLineItemViewModelTests()
    {
        _originalCulture = CultureInfo.CurrentCulture;
        SiteCurrency.Reset();
    }

    public void Dispose()
    {
        SiteCurrency.Reset();
        CultureInfo.CurrentCulture = _originalCulture;
    }

    private class FakeInvoiceLineHost : IInvoiceLineHost
    {
        public List<(InvoiceLineItemViewModel Line, string Property)> ChangedLines { get; } = new();
        public List<InvoiceLineItemViewModel> EditRequestedLines { get; } = new();
        public List<InvoiceLineItemViewModel> ValidatedLines { get; } = new();
        public List<InvoiceLineItemViewModel> PhotoRequestedLines { get; } = new();

        public void OnLineChanged(InvoiceLineItemViewModel line, string property) =>
            ChangedLines.Add((line, property));

        public void OnLineEditRequested(InvoiceLineItemViewModel line) =>
            EditRequestedLines.Add(line);

        public Task ValidateLineAsync(InvoiceLineItemViewModel line)
        {
            ValidatedLines.Add(line);
            line.SetValidated(true);
            return Task.CompletedTask;
        }

        public Task AddLinePhotoAsync(InvoiceLineItemViewModel line)
        {
            PhotoRequestedLines.Add(line);
            return Task.CompletedTask;
        }
    }

    private static (InvoiceExtractedLine Read, InvoiceReviewLine State) CreateSampleLine(
        bool hasDoubt = false,
        string? unitCost = "12,50",
        string? quantity = "2",
        string? coefficient = "2")
    {
        var read = new InvoiceExtractedLine
        {
            N = 1,
            Label = "Robe lin M",
            UnitCost = hasDoubt ? null : 1250L,
            UnitCostRead = 1250L,
            Confidence = 0.95,
            Quantity = 2,
            GroupKey = "robe-lin",
            Product = hasDoubt ? null : new InvoiceProductMatch { Id = 10, Name = "Robe lin", Score = 0.95 },
            Candidates = new List<InvoiceProductMatch>()
        };

        var state = new InvoiceReviewLine
        {
            N = 1,
            Label = "Robe lin M",
            Quantity = quantity,
            UnitCost = unitCost,
            Coefficient = coefficient,
            Mode = InvoiceReviewDraft.ModeNew,
            Name = "Robe lin",
            Validated = false
        };

        return (read, state);
    }

    [Fact]
    public void IsNew_And_IsExisting_ReflectSelectedLink()
    {
        var host = new FakeInvoiceLineHost();
        var (read, state) = CreateSampleLine();
        var vm = new InvoiceLineItemViewModel(host, read, state);

        // Initialement avec "Nouvel article"
        Assert.True(vm.IsNew);
        Assert.False(vm.IsExisting);
        Assert.Equal("Nouvel article", vm.StatusText);

        // Sélection d'un article déjà connu
        var existingOption = InvoiceLinkOption.Existing(42, "Robe en lin", "ROB-42", 101);
        vm.SelectedLink = existingOption;

        Assert.False(vm.IsNew);
        Assert.True(vm.IsExisting);
        Assert.Contains("Robe en lin", vm.StatusText);
        Assert.Equal("Prix de vente : inchangé", vm.SalePriceText);
    }

    [Fact]
    public void ShowDoubt_WhenDoubtPresentAndNotValidated_ReturnsTrue_UntilValidated()
    {
        var host = new FakeInvoiceLineHost();
        var (readWithDoubt, state) = CreateSampleLine(hasDoubt: true);
        var vmWithDoubt = new InvoiceLineItemViewModel(host, readWithDoubt, state);

        Assert.NotNull(vmWithDoubt.DoubtReason);
        Assert.False(vmWithDoubt.IsValidated);
        Assert.True(vmWithDoubt.ShowDoubt);

        // Validation de la ligne
        vmWithDoubt.SetValidated(true);
        Assert.True(vmWithDoubt.IsValidated);
        Assert.False(vmWithDoubt.ShowDoubt);

        // Ligne sans doute
        var (readClean, stateClean) = CreateSampleLine(hasDoubt: false);
        var vmClean = new InvoiceLineItemViewModel(host, readClean, stateClean);
        Assert.Null(vmClean.DoubtReason);
        Assert.False(vmClean.ShowDoubt);
    }

    [Fact]
    public void ToLine_CapturesAllEnteredValues()
    {
        var host = new FakeInvoiceLineHost();
        var (read, state) = CreateSampleLine();
        var vm = new InvoiceLineItemViewModel(host, read, state);

        vm.Name = "Robe été";
        vm.QuantityText = "5";
        vm.UnitCostText = "15,00";
        vm.CoefficientText = "2,5";
        vm.Size = "L";
        vm.Color = "Bleu";
        vm.SetValidated(true);

        var line = vm.ToLine();

        Assert.NotNull(line);
        Assert.Equal(1, line.N);
        Assert.True(line.Validated);
        Assert.Equal("Robe été", line.Name);
        Assert.Equal("5", line.Quantity);
        Assert.Equal("15,00", line.UnitCost);
        Assert.Equal("2,5", line.Coefficient);
        Assert.Equal("L", line.Size);
        Assert.Equal("Bleu", line.Color);
        Assert.Equal(InvoiceReviewDraft.ModeNew, line.Mode);
    }

    [Fact]
    public void ApplyGroupValues_UpdatesNameAndCoefficient_WithoutTriggeringHostChange()
    {
        var host = new FakeInvoiceLineHost();
        var (read, state) = CreateSampleLine();
        var vm = new InvoiceLineItemViewModel(host, read, state);

        vm.SetValidated(true);
        host.ChangedLines.Clear();

        vm.ApplyGroupValues("Robe groupe", "2,8");

        Assert.Equal("Robe groupe", vm.Name);
        Assert.Equal("2,8", vm.CoefficientText);
        Assert.True(vm.IsValidated); // La validation n'est pas réinitialisée par ApplyGroupValues
        Assert.Empty(host.ChangedLines); // Aucune notification envoyée à l'hôte pendant _applying
    }

    [Fact]
    public void SetCurrency_DocumentDiffersFromSite_WithoutRate_ShowsNoticeInSalePrice()
    {
        SiteCurrency.SetCurrency("EUR");
        CultureInfo.CurrentCulture = new CultureInfo("fr-FR");

        var host = new FakeInvoiceLineHost();
        var (read, state) = CreateSampleLine(unitCost: "10,00", coefficient: "2");
        var vm = new InvoiceLineItemViewModel(host, read, state);

        vm.SetCurrency("USD", "EUR", exchangeRate: null);

        // QuantityCostText affiche la devise du document USD
        Assert.Contains("$", vm.QuantityCostText);
        Assert.DoesNotContain("€", vm.QuantityCostText);
        Assert.Contains("10", vm.QuantityCostText);

        // SalePriceText indique « avant taux de change » en USD
        Assert.Contains("avant taux de change", vm.SalePriceText);
        Assert.Contains("$", vm.SalePriceText);
        Assert.DoesNotContain("€", vm.SalePriceText);
        Assert.Contains("20", vm.SalePriceText); // 10 × 2
    }

    [Fact]
    public void SetCurrency_DocumentDiffersFromSite_WithRate_ConvertsPriceToSiteCurrency()
    {
        SiteCurrency.SetCurrency("EUR");
        CultureInfo.CurrentCulture = new CultureInfo("fr-FR");

        var host = new FakeInvoiceLineHost();
        var (read, state) = CreateSampleLine(unitCost: "10,00", coefficient: "2");
        var vm = new InvoiceLineItemViewModel(host, read, state);

        // Taux 0.90 USD -> EUR : 1000 * 2 * 0.90 = 1800 centimes (18 €)
        vm.SetCurrency("USD", "EUR", exchangeRate: 0.90);

        Assert.Contains("€", vm.SalePriceText);
        Assert.DoesNotContain("$", vm.SalePriceText);
        Assert.DoesNotContain("avant taux de change", vm.SalePriceText);
        Assert.Contains("18", vm.SalePriceText);
        Assert.Contains("coût ×", vm.SalePriceText);
    }
}
