using Easymakemoney.Models;
using Easymakemoney.Services;
using Easymakemoney.Tests.Fakes;
using Easymakemoney.ViewModels.Lists;
using Newtonsoft.Json;
using Xunit;

namespace Easymakemoney.Tests.ViewModels;

/// <summary>Écran « Notifications » du mobile (lot backend NOTIF-1) : niveau du compte, « Comme le site », types avancés.</summary>
public class NotificationPreferencesTests
{
    private const string Json = """
    { "level": { "value": "essential", "source": "site", "userLevel": null,
        "siteLevel": { "value": "essential", "mode": "auto", "reason": "platform_demo", "label": "Essentiel", "reasonLabel": "site d'essai" },
        "dailyCap": 3, "heldCount": 2, "siteChoiceLabel": "Comme le site",
        "choices": [ { "key": "none", "label": "Aucune", "description": "Seulement l'indispensable." },
                     { "key": "essential", "label": "Essentiel", "description": "L'urgent, et un résumé par semaine." },
                     { "key": "normal", "label": "Normal", "description": "Un courriel par jour et par sujet." },
                     { "key": "all", "label": "Tout", "description": "Chaque événement." } ] },
      "kinds": [ { "kind": "low_stock", "label": "Stock bas", "enabled": true, "available": true, "channel": "push", "family": "followup" },
                 { "kind": "loss_digest", "label": "Résumé des pertes", "enabled": true, "available": false, "channel": "email", "family": "followup" },
                 { "kind": "weekly_digest", "label": "Point de la semaine", "enabled": true, "available": true, "channel": "email", "family": "followup" } ] }
    """;

    private static NotificationPreferences Read() => JsonConvert.DeserializeObject<NotificationPreferences>(Json)!;

    [Fact]
    public void Choices_SiteFirst_CurrentChoiceSelected_AndTexts()
    {
        var preferences = Read();
        var choices = preferences.Choices();

        Assert.Equal(new string?[] { null, "none", "essential", "normal", "all" }, choices.Select(c => c.Key));
        Assert.True(choices[0].IsSelected);
        Assert.Equal("Niveau du site : Essentiel (site d'essai)", choices[0].Description);
        Assert.Equal("Au plus 3 courriels de suivi par jour.", preferences.CapText);
        Assert.Equal("2 courriels retenus pour le prochain résumé.", preferences.HeldText);
    }

    [Fact]
    public async Task SelectLevel_SendsTheLevel_SiteChoiceSendsNull_UnavailableKindsHidden()
    {
        var http = new FakeHttpService { ResultToReturn = Read() };
        var vm = new NotificationPreferencesViewModel(new NotificationPreferencesService(http), new FakeDialogService());
        await vm.LoadAsync();

        Assert.Equal(new[] { "low_stock", "weekly_digest" }, vm.Kinds.Select(k => k.kind));

        await vm.SelectLevelCommand.ExecuteAsync(vm.Choices.First(c => c.Key == "normal"));
        var body = Assert.IsType<Dictionary<string, object?>>(http.PostedAuthData.Last().Data);
        Assert.Equal("normal", body["level"]);

        await vm.ToggleKindCommand.ExecuteAsync(vm.Kinds.First(k => k.kind == "weekly_digest"));
        var kinds = Assert.IsType<Dictionary<string, object>>(http.PostedAuthData.Last().Data);
        Assert.Equal(false, Assert.IsType<Dictionary<string, bool>>(kinds["kinds"])["weekly_digest"]);
        Assert.All(http.PostedAuthData, call => Assert.Equal("gestion/notifications/preferences?locale=fr", call.Url));
    }
}
