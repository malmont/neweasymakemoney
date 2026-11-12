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

    /// <summary>Saisies demandées ; réponses rendues dans l'ordre (PromptResults), sinon PromptResult (null = annulé).</summary>
    public List<(string Title, string Message, string? InitialValue)> PromptCalls { get; } = new();
    public Queue<string?> PromptResults { get; } = new();
    public string? PromptResult { get; set; }

    /// <summary>Choix demandés ; réponses rendues dans l'ordre (ChooseResults), sinon ChooseResult (null = annulé).</summary>
    public List<(string Title, IReadOnlyList<string> Choices)> ChooseCalls { get; } = new();
    public Queue<string?> ChooseResults { get; } = new();
    public string? ChooseResult { get; set; }

    public Task<string?> PromptAsync(string title, string message, string accept = "Valider", string cancel = "Annuler",
        string? initialValue = null, Keyboard? keyboard = null, string? placeholder = null)
    {
        PromptCalls.Add((title, message, initialValue));
        return Task.FromResult(PromptResults.Count > 0 ? PromptResults.Dequeue() : PromptResult);
    }

    public Task<string?> ChooseAsync(string title, IReadOnlyList<string> choices, string cancel = "Annuler", string? message = null)
    {
        ChooseCalls.Add((title, choices));
        return Task.FromResult(ChooseResults.Count > 0 ? ChooseResults.Dequeue() : ChooseResult);
    }

    public Task<bool> ConfirmAsync(string title, string message, string accept = "Oui", string cancel = "Non")
    {
        ConfirmCalls.Add((title, message, accept, cancel));
        if (ConfirmResults.Count > 0)
            return Task.FromResult(ConfirmResults.Dequeue());
        return Task.FromResult(ConfirmResult);
    }
}

