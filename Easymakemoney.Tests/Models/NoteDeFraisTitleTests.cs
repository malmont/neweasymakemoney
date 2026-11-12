using Easymakemoney.Models;
using Newtonsoft.Json;
using Xunit;

namespace Easymakemoney.Tests.Models;

/// <summary>Le serveur renvoie « description » sans « name » (NoteDeFraisOutputDTO) : la ligne de la note ne doit pas être vide.</summary>
public class NoteDeFraisTitleTests
{
    [Fact]
    public void ServerNote_WithoutName_ShowsDescription()
    {
        var note = JsonConvert.DeserializeObject<NoteDeFrais>("""
        { "id": 3, "description": "Taxi aéroport", "montant": 4500, "currency": "CAD", "date": "2026-11-10", "imageNdf": null, "document": null, "tripId": null }
        """)!;

        Assert.Equal("Taxi aéroport", note.DisplayTitle);
    }

    [Fact]
    public void LocalNote_WithName_KeepsName()
    {
        Assert.Equal("Repas", new NoteDeFrais { name = "Repas", description = "Midi" }.DisplayTitle);
    }

    [Fact]
    public void DisplayTitle_IsNeverSent()
    {
        var json = JsonConvert.SerializeObject(new NoteDeFrais { name = "a", description = "b", date = "2026-11-10" });

        Assert.DoesNotContain("DisplayTitle", json);
    }
}
