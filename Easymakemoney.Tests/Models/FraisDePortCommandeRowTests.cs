using Easymakemoney.Models;
using Xunit;

namespace Easymakemoney.Tests.Models;

public class FraisDePortCommandeRowTests
{
    private static ListCommand MakeCommand(string name = "Commande Test") => new()
    {
        name = name,
        date = "2026-10-15T00:00:00Z",
        photo = "photo.png",
        collectionId = 1,
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
}
