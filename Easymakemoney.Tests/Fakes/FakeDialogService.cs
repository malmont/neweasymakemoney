using Easymakemoney.Services.Dialog;

namespace Easymakemoney.Tests.Fakes;

public class FakeDialogService : IDialogService
{
    public List<Exception> ShownErrors { get; } = new();
    public List<string> ShownInfos { get; } = new();
    public List<(string Title, string Message, string Accept, string Cancel)> ConfirmCalls { get; } = new();
    public bool ConfirmResult { get; set; } = true;

    public Task ShowErrorAsync(Exception exception)
    {
        ShownErrors.Add(exception);
        return Task.CompletedTask;
    }

    public Task ShowInfoAsync(string message, string title = "Information")
    {
        ShownInfos.Add(message);
        return Task.CompletedTask;
    }

    public Task<bool> ConfirmAsync(string title, string message, string accept = "Oui", string cancel = "Non")
    {
        ConfirmCalls.Add((title, message, accept, cancel));
        return Task.FromResult(ConfirmResult);
    }
}

