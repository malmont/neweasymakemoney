using System.Globalization;
using Easymakemoney.Models;
using Easymakemoney.Utilities;
using Xunit;

namespace Easymakemoney.Tests.Models;

[Collection("SiteCurrency")]
public class FraisDePortCommandeRowTests : IDisposable
{
    private readonly CultureInfo _originalCulture;

    public FraisDePortCommandeRowTests()
    {
        _originalCulture = CultureInfo.CurrentCulture;
        SiteCurrency.Reset();
    }

    public void Dispose()
    {
        SiteCurrency.Reset();
        CultureInfo.CurrentCulture = _originalCulture;
    }

    private static ListCommand MakeCommand(string name = "Commande Test", string? currency = null) => new()
    {
        name = name,
        date = "2026-10-15T00:00:00Z",
        photo = "photo.png",
        collectionId = 1,
        currency = currency,
        fournisseur = new ListFournisseur { name = "Fournisseur A" }
    };

    [Fact]
    public void PeriodClosed_PreventsBothAddAndDelete_RegardlessOfFrais()
    {
        // Période fermée sans frais
        var rowWithoutFrais = new FraisDePortCommandeRow
        {
            Commande = MakeCommand(),
            Frais = null,
            PeriodClosed = true
        };

        Assert.False(rowWithoutFrais.CanAddFrais);
        Assert.False(rowWithoutFrais.CanDeleteFrais);
        Assert.False(rowWithoutFrais.HasFrais);

        // Période fermée avec frais
        var rowWithFrais = new FraisDePortCommandeRow
        {
            Commande = MakeCommand(),
            Frais = new FraisDePort { name = "Colissimo", image = "colissimo.png", price = 1250 },
            PeriodClosed = true
        };

        Assert.False(rowWithFrais.CanAddFrais);
        Assert.False(rowWithFrais.CanDeleteFrais);
        Assert.True(rowWithFrais.HasFrais);
    }

    [Fact]
    public void PeriodOpen_WithoutFrais_AllowsAddOnly()
    {
        var row = new FraisDePortCommandeRow
        {
            Commande = MakeCommand(),
            Frais = null,
            PeriodClosed = false
        };

        Assert.True(row.CanAddFrais);
        Assert.False(row.CanDeleteFrais);
        Assert.Equal("Aucun frais de port", row.DetailText);
    }

    [Fact]
    public void PeriodOpen_WithFrais_AllowsDeleteOnly()
    {
        var row = new FraisDePortCommandeRow
        {
            Commande = MakeCommand(),
            Frais = new FraisDePort
            {
                name = "Express",
                image = "express.png",
                price = 2500,
                transporteur = new Transporteur { name = "DHL" }
            },
            PeriodClosed = false
        };

        Assert.False(row.CanAddFrais);
        Assert.True(row.CanDeleteFrais);
        Assert.Equal("DHL · Express", row.DetailText);
        Assert.False(string.IsNullOrEmpty(row.PriceText));
    }

    [Fact]
    public void DetailText_WithoutTransporteur_ReturnsFraisName()
    {
        var row = new FraisDePortCommandeRow
        {
            Commande = MakeCommand(),
            Frais = new FraisDePort
            {
                name = "Livraison directe",
                image = "livraison.png",
                price = 800,
                transporteur = null
            },
            PeriodClosed = false
        };

        Assert.Equal("Livraison directe", row.DetailText);
    }

    [Fact]
    public void PriceText_WhenFraisNull_ReturnsEmpty()
    {
        var row = new FraisDePortCommandeRow
        {
            Commande = MakeCommand(),
            Frais = null
        };

        Assert.Equal(string.Empty, row.PriceText);
    }

    [Fact]
    public void PriceText_WhenCommandeCurrencySpecified_FormatsInCommandCurrencyNotSiteCurrency()
    {
        SiteCurrency.SetCurrency("EUR");
        CultureInfo.CurrentCulture = new CultureInfo("fr-FR");

        var row = new FraisDePortCommandeRow
        {
            Commande = MakeCommand(currency: "USD"),
            Frais = new FraisDePort { name = "Express", image = "img.png", price = 2500 }
        };

        var text = row.PriceText;
        Assert.False(string.IsNullOrWhiteSpace(text));
        Assert.True(text.Contains("$") || text.Contains("US$"));
        Assert.DoesNotContain("€", text);
        Assert.Contains("25", text);
    }

    [Fact]
    public void PriceText_WhenCommandeCurrencyNull_FallsBackToSiteCurrency()
    {
        SiteCurrency.SetCurrency("EUR");
        CultureInfo.CurrentCulture = new CultureInfo("fr-FR");

        var row = new FraisDePortCommandeRow
        {
            Commande = MakeCommand(currency: null),
            Frais = new FraisDePort { name = "Express", image = "img.png", price = 2500 }
        };

        var text = row.PriceText;
        Assert.False(string.IsNullOrWhiteSpace(text));
        Assert.Contains("€", text);
        Assert.DoesNotContain("$", text);
        Assert.Contains("25", text);
    }
}
