namespace Easymakemoney.Services.Dialog;

public interface IDialogService
{
    Task ShowErrorAsync(Exception exception);
    Task ShowInfoAsync(string message, string title = "Information");
    Task<bool> ConfirmAsync(string title, string message, string accept = "Oui", string cancel = "Non");

    /// <summary>Une saisie au style de l'appli ; le texte saisi, ou null si le gérant annule.</summary>
    Task<string?> PromptAsync(string title, string message, string accept = "Valider", string cancel = "Annuler",
        string? initialValue = null, Keyboard? keyboard = null, string? placeholder = null);

    /// <summary>Un choix dans une liste, au style de l'appli ; la réponse choisie, ou null si le gérant annule.</summary>
    Task<string?> ChooseAsync(string title, IReadOnlyList<string> choices, string cancel = "Annuler", string? message = null);
}

