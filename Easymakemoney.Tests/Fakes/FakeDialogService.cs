using Easymakemoney.Services.Dialog;

namespace Easymakemoney.Tests.Fakes;

public class FakeDialogService : IDialogService
{
    public List<Exception> ShownErrors { get; } = new();
    public List<string> ShownInfos { get; } = new();
    public List<(string Title, string Message, string Accept, string Cancel)> ConfirmCalls { get; } = new();
    public bool ConfirmResult { get; set; } = true;
    public Queue<bool> ConfirmResults { get; } = new();

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
        if (ConfirmResults.Count > 0)
            return Task.FromResult(ConfirmResults.Dequeue());
        return Task.FromResult(ConfirmResult);
    }
}

