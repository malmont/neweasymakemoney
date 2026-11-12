using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Easymakemoney.Models.Tenant;
using Easymakemoney.Services.Site;
using Easymakemoney.Services.Tenant;
using Easymakemoney.Views.Startup;

namespace Easymakemoney.ViewModels.Startup;

public partial class ActivationPageViewModel : BaseViewModel
{
        /// <summary>Navigation (point d'entrée des tests).</summary>
        public Func<string, Task> NavigateAsync { get; set; } = route => Shell.Current?.GoToAsync(route) ?? Task.CompletedTask;

    private readonly ISiteService _siteService;
    private readonly ITenantStore _tenantStore;
    private readonly ISiteDataCleaner _siteDataCleaner;

    [ObservableProperty]
    private string _keyInput = string.Empty;

    [ObservableProperty]
    private string _siteCodeInput = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasErrorMessage))]
    private string? _errorMessage;

    public bool HasErrorMessage => !string.IsNullOrWhiteSpace(ErrorMessage);

    [ObservableProperty]
    private bool _isConfirming;

    [ObservableProperty]
    private TenantContext? _candidateTenant;

    public ActivationPageViewModel(
        ISiteService siteService,
        ITenantStore tenantStore,
        ISiteDataCleaner siteDataCleaner)
    {
        _siteService = siteService;
        _tenantStore = tenantStore;
        _siteDataCleaner = siteDataCleaner;
        Title = "Configuration du site";
    }

    [RelayCommand]
    private async Task ValidateKeyAsync()
    {
        if (IsBusy) return;

        IsBusy = true;
        ErrorMessage = null;

        try
        {
            var result = await _siteService.ActivateWithKeyAsync(KeyInput);
            if (result.IsSuccess && result.Tenant != null)
            {
                CandidateTenant = result.Tenant;
                IsConfirming = true;
            }
            else
            {
                ErrorMessage = result.ErrorMessage;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ActivationPageViewModel] Erreur ValidateKeyAsync : {ex}");
            ErrorMessage = "Une erreur est survenue, réessayez.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ValidateSiteCodeAsync()
    {
        if (IsBusy) return;

        IsBusy = true;
        ErrorMessage = null;

        try
        {
            var result = await _siteService.CheckSiteCodeAsync(SiteCodeInput);
            if (result.IsSuccess && result.Tenant != null)
            {
                CandidateTenant = result.Tenant;
                IsConfirming = true;
            }
            else
            {
                ErrorMessage = result.ErrorMessage;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ActivationPageViewModel] Erreur ValidateSiteCodeAsync : {ex}");
            ErrorMessage = "Une erreur est survenue, réessayez.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ConfirmTenantAsync()
    {
        if (CandidateTenant == null || IsBusy) return;

        IsBusy = true;
        try
        {
            // Règle L7 : effacer toute session et données préalables avant d'enregistrer le nouveau site
            await _siteDataCleaner.ClearAllAsync();
            await _tenantStore.SaveTenantAsync(CandidateTenant);
            IsConfirming = false;

            await NavigateAsync("//LoginPage");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ActivationPageViewModel] Erreur ConfirmTenantAsync : {ex}");
            ErrorMessage = "Une erreur est survenue, réessayez.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void CancelConfirmation()
    {
        CandidateTenant = null;
        IsConfirming = false;
        ErrorMessage = null;
    }
}
