using Easymakemoney.Models;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Easymakemoney.Tests.Models;

public class CommandeReceptionTests
{
    [Fact]
    public void Read_ReceivedLinesAndNeedsVariant()
    {
        var json = JObject.Parse("""
        {
          "received": [ { "lineId": 10, "quantity": 3, "movementId": 100 }, { "lineId": 11, "quantity": 2, "movementId": 101 } ],
          "needsVariant": [
            { "lineId": 12, "product": { "id": 5, "name": "Robe lin", "code": "RL" },
              "variants": [ { "id": 51, "size": "M", "color": "Rouge" }, { "id": 52, "size": "L", "color": null } ] }
          ],
          "lines": [],
          "commande": { "id": 7, "remaining": 4, "receptionStatus": "partial" }
        }
        """);

        var reception = CommandeReception.Read(json);

        Assert.Equal(2, reception.ReceivedLines);
        Assert.Equal(5, reception.ReceivedQuantity);
        Assert.Equal(7, reception.CommandeId);
        Assert.Equal(4, reception.Remaining);
        Assert.Equal("partial", reception.ReceptionStatus);

        var need = Assert.Single(reception.NeedsVariant);
        Assert.Equal(12, need.LineId);
        Assert.Equal("Robe lin", need.ProductName);
        Assert.Equal(new[] { "Taille M · Couleur Rouge", "Taille L" }, need.ChoiceLabels());
    }

    [Fact]
    public void Read_AlreadyReceived_IsEmpty()
    {
        var json = JObject.Parse("""{ "received": [], "needsVariant": [], "lines": [], "commande": { "id": 7, "remaining": 0, "receptionStatus": "complete" } }""");
        var reception = CommandeReception.Read(json);

        Assert.Equal(0, reception.ReceivedQuantity);
        Assert.Empty(reception.NeedsVariant);
        Assert.Equal("Rien de nouveau à recevoir : le stock est déjà à jour. La commande est entièrement reçue.",
            CommandeReception.Summary(0, reception, 0));
    }

    [Fact]
    public void Read_ToleratesMissingOrOddFields()
    {
        Assert.Empty(CommandeReception.Read(null).NeedsVariant);
        var reception = CommandeReception.Read(JObject.Parse("""{ "received": 3, "needsVariant": [ { "lineId": null }, { "lineId": 4 } ] }"""));
        Assert.Equal(3, reception.ReceivedQuantity);
        var need = Assert.Single(reception.NeedsVariant);
        Assert.Empty(need.Variants);
        Assert.Null(reception.Remaining);
    }

    [Fact]
    public void ChoiceLabels_AreUnique()
    {
        var need = new CommandeReceptionNeedsVariant
        {
            LineId = 1,
            Variants = { new CommandeReceptionVariant { Id = 1, Size = "M" }, new CommandeReceptionVariant { Id = 2, Size = "M" }, new CommandeReceptionVariant { Id = 3 } },
        };
        Assert.Equal(new[] { "Taille M (n° 1)", "Taille M (n° 2)", "Variante n° 3" }, need.ChoiceLabels());
    }

    [Fact]
    public void VariantChoicesBody_ListsLineAndVariant()
    {
        var body = CommandeReception.VariantChoicesBody(new[] { (12, 51), (13, 60) });
        var lines = (JArray)body["lines"]!;
        Assert.Equal(2, lines.Count);
        Assert.Equal(12, lines[0]["lineId"]!.Value<int>());
        Assert.Equal(51, lines[0]["variantId"]!.Value<int>());
        Assert.Equal(60, lines[1]["variantId"]!.Value<int>());
    }

    [Fact]
    public void Summary_CountsAndRemaining()
    {
        var partial = new CommandeReception { Remaining = 1, ReceptionStatus = "partial" };
        Assert.Equal("Réception enregistrée : 6 articles ajoutés au stock. Il reste 1 article à recevoir.",
            CommandeReception.Summary(6, partial, 1));

        var complete = new CommandeReception { Remaining = 0, ReceptionStatus = "complete" };
        Assert.Equal("Réception enregistrée : 1 article ajouté au stock. La commande est entièrement reçue.",
            CommandeReception.Summary(1, complete, 0));
    }
}
