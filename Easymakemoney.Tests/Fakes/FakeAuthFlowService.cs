using Easymakemoney.Services.Auth;

namespace Easymakemoney.Tests.Fakes;

/// <summary>Faux service de flux d'authentification pour isoler LoginPageViewModel lors du changement de site.</summary>
public class FakeAuthFlowService : IAuthFlowService
{
    public int LogoutCalls { get; private set; }
    public int PromptAndChangeSiteCalls { get; private set; }
    public bool ChangeSiteResult { get; set; }

    public Task LogoutAsync()
    {
        LogoutCalls++;
        return Task.CompletedTask;
    }

    public Task<bool> PromptAndChangeSiteAsync()
    {
        PromptAndChangeSiteCalls++;
        return Task.FromResult(ChangeSiteResult);
    }
}

