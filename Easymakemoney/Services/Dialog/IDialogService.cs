namespace Easymakemoney.Services.Dialog;

public interface IDialogService
{
    Task ShowErrorAsync(Exception exception);
    Task ShowInfoAsync(string message, string title = "Information");
    Task<bool> ConfirmAsync(string title, string message, string accept = "Oui", string cancel = "Non");
}

