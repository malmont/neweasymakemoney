using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Easymakemoney.Controls;
using Easymakemoney.Services.Auth;

namespace Easymakemoney.ViewModels.Startup;

public partial class OtpPageViewModel : BaseViewModel
{
    private readonly LoginUseCase _loginUseCase;
    private readonly IPendingLoginStore _pendingLoginStore;

    [ObservableProperty]
    private string _email = string.Empty;

    [ObservableProperty]
    private string _otpCode = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasErrorMessage))]
    private string? _errorMessage;

    public bool HasErrorMessage => !string.IsNullOrWhiteSpace(ErrorMessage);

    public OtpPageViewModel(
        LoginUseCase loginUseCase,
        IPendingLoginStore pendingLoginStore)
    {
        _loginUseCase = loginUseCase;
        _pendingLoginStore = pendingLoginStore;
        Title = "Vérification du code";
    }

    public async Task InitializeAsync()
    {
        if (_pendingLoginStore.TryGet(out var email, out _))
        {
            Email = email;
            ErrorMessage = null;
            OtpCode = string.Empty;
        }
        else
        {
            // Aucun identifiant en attente ou expiré depuis plus de 10 minutes
            if (Shell.Current != null) await Shell.Current.GoToAsync("//LoginPage");
        }
    }

    [RelayCommand]
    private async Task VerifyOtpAsync()
    {
        if (IsBusy) return;

        IsBusy = true;
        ErrorMessage = null;

        try
        {
            if (!_pendingLoginStore.TryGet(out var email, out _))
            {
                if (Shell.Current != null) await Shell.Current.GoToAsync("//LoginPage");
                return;
            }

            var result = await _loginUseCase.ExecuteVerifyOtpAsync(email, OtpCode);

            if (result.IsSuccess)
            {
                _pendingLoginStore.Clear();
                OtpCode = string.Empty;

                if (AppShell.Current != null && result.User != null)
                {
                    AppShell.Current.FlyoutHeader = new FlyoutHeaderControl(result.User);
                }

                if (Shell.Current != null) await Shell.Current.GoToAsync("//DashboardPage");
            }
            else
            {
                ErrorMessage = result.ErrorMessage;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[OtpPageViewModel] Erreur validation OTP : {ex.Message}");
            ErrorMessage = "Une erreur est survenue, réessayez.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ResendOtpAsync()
    {
        if (IsBusy) return;

        IsBusy = true;
        ErrorMessage = null;

        try
        {
            if (!_pendingLoginStore.TryGet(out var email, out var password))
            {
                if (Shell.Current != null) await Shell.Current.GoToAsync("//LoginPage");
                return;
            }

            var result = await _loginUseCase.ExecuteLoginAsync(email, password);

            if (result.OtpRequired)
            {
                OtpCode = string.Empty;
                ErrorMessage = null;
                // Renouveler l'heure de stockage
                _pendingLoginStore.Set(email, password);
                await Easymakemoney.Utilities.AppDialogs.InfoAsync("Code envoyé", $"Un nouveau code a été envoyé à l'adresse {email}.");
            }
            else if (result.IsSuccess)
            {
                _pendingLoginStore.Clear();
                if (AppShell.Current != null && result.User != null)
                {
                    AppShell.Current.FlyoutHeader = new FlyoutHeaderControl(result.User);
                }
                if (Shell.Current != null) await Shell.Current.GoToAsync("//DashboardPage");
            }
            else
            {
                ErrorMessage = result.ErrorMessage;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[OtpPageViewModel] Erreur renvoi OTP : {ex.Message}");
            ErrorMessage = "Une erreur est survenue, réessayez.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task BackAsync()
    {
        if (IsBusy) return;

        _pendingLoginStore.Clear();
        OtpCode = string.Empty;
        ErrorMessage = null;

        if (Shell.Current != null) await Shell.Current.GoToAsync("//LoginPage");
    }
}

