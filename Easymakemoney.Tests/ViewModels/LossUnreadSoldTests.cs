using Easymakemoney.Models;
using Newtonsoft.Json;
using Xunit;

namespace Easymakemoney.Tests.ViewModels;

/// <summary>Règle UNREAD_SOLD (backend L3-b2b) : le constat d'un poste de caisse se lit sans changement de l'appli, sans personne.</summary>
public class LossUnreadSoldTests
{
    private const string Json = """
    {"id": 13, "number": -2, "rule": {"code": "UNREAD_SOLD", "label": "Articles suivis vendus sans lecture"}, "personPattern": false, "status": "open", "statusLabel": "Nouveau",
     "summary": "Articles suivis vendus sans lecture ce jour-là à ce poste : 6 sur 30 (20 %).", "storeDay": "2026-11-16", "cashRegister": null, "amountAtStake": null, "currency": "CAD",
     "isNew": false, "newEvidenceSinceDecision": false, "backfill": false, "exercise": true, "subject": {"type": "cash_register", "id": 0, "name": null, "label": "Poste de caisse"},
     "onDuty": [], "onDutyNotice": null, "amountNotice": "Le montant en jeu est le montant concerné par le constat, pas une perte prouvée.",
     "evidence": [{"id": 31, "kind": "sale_units", "at": "2026-11-16T07:53:13+00:00", "atLabel": "16/11/2026 02 h 53", "title": "Articles suivis vendus sur le poste ce jour-là",
       "lines": [{"label": "Articles suivis vendus", "value": "30"}, {"label": "Vendus sans lecture", "value": "6"}]}],
     "evidenceIntact": true, "opinion": null, "decision": null, "history": [], "screen": "pertes.exercices"}
    """;

    [Fact]
    public void PostFinding_ReadsWithoutPeople_AndWithoutAmount()
    {
        var finding = JsonConvert.DeserializeObject<LossFindingDetail>(Json)!;

        Assert.Equal("Exercice 2 · Articles suivis vendus sans lecture", finding.HeaderText);
        Assert.DoesNotContain("en jeu", finding.DetailText);
        Assert.Empty(finding.onDuty);
        Assert.Equal(2, finding.evidence.Single().lines.Count);
    }
}
