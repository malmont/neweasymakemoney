using Xunit;
using Easymakemoney.Models;
using Easymakemoney.Utilities;

namespace Easymakemoney.Tests.Models;

/// <summary>Règle de l'App Store : sur iPhone, ni essai, ni demande, ni renvoi vers le web pour activer un module.</summary>
[Collection(AppStorePolicyCollection.Name)]
public class AppStorePolicyTests : IDisposable
{
    private readonly Func<bool> _original = AppStorePolicy.IsIos;

    public void Dispose() => AppStorePolicy.IsIos = _original;

    [Fact]
    public void Sur_iPhone_aucun_bouton_ni_renvoi()
    {
        AppStorePolicy.IsIos = () => true;
        var info = new ModuleInactiveInfo { Requestable = true, TrialDays = 30 };

        Assert.False(AppStorePolicy.CanOfferModules);
        Assert.False(info.CanRequest);
        Assert.Equal("L'application de gestion n'est pas encore activée sur ce site.", info.Explanation);
        Assert.Equal("Ce site n'a pas encore la Prévention des pertes.", AppStorePolicy.LossModuleMissingText);
        Assert.Equal("Ce site n'a pas le module Assistance.", AppStorePolicy.SupportModuleMissingText);
        foreach (var text in new[] { info.Explanation, AppStorePolicy.LossModuleMissingText, AppStorePolicy.SupportModuleMissingText })
        {
            Assert.DoesNotContain("essai", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("web", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Mon site", text);
        }
    }

    [Fact]
    public void Sur_iPhone_une_demande_en_attente_est_dite_sans_bouton()
    {
        AppStorePolicy.IsIos = () => true;
        var info = new ModuleInactiveInfo { Requestable = true, Pending = true };
        Assert.False(info.CanRequest);
        Assert.Equal("Une demande d'activation est en attente pour ce site.", info.Explanation);
    }

    [Fact]
    public void Sur_Android_l_essai_reste_propose()
    {
        AppStorePolicy.IsIos = () => false;
        var info = new ModuleInactiveInfo { Requestable = true, TrialDays = 30 };

        Assert.True(info.CanRequest);
        Assert.Equal("Essayer 30 jours gratuitement", info.RequestButtonText);
        Assert.Contains("s'essaie gratuitement", AppStorePolicy.LossModuleMissingText);
        Assert.Contains("« Mon site »", AppStorePolicy.SupportModuleMissingText);
    }
}

[CollectionDefinition(Name, DisableParallelization = true)]
public class AppStorePolicyCollection
{
    public const string Name = "AppStorePolicy";
}
