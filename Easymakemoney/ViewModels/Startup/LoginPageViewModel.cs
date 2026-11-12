using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Easymakemoney.Controls;
using Easymakemoney.Services.Auth;
using Easymakemoney.Services.Session;

namespace Easymakemoney.ViewModels.Startup
{
    public partial class LoginPageViewModel : BaseViewModel
    {
        [ObservableProperty]
        private string _email = string.Empty;

        [ObservableProperty]
        private string _password = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasErrorMessage))]
        private string? _errorMessage;

        public bool HasErrorMessage => !string.IsNullOrWhiteSpace(ErrorMessage);

        private readonly LoginUseCase _loginUseCase;
        private readonly IAuthFlowService _authFlowService;
        private readonly IPendingLoginStore _pendingLoginStore;
        private readonly ISessionService _sessionService;

        public LoginPageViewModel(
            LoginUseCase loginUseCase,
            IAuthFlowService authFlowService,
            IPendingLoginStore pendingLoginStore,
            ISessionService sessionService)
        {
            _loginUseCase = loginUseCase;
            _authFlowService = authFlowService;
            _pendingLoginStore = pendingLoginStore;
            _sessionService = sessionService;

            _ = InitializeAsync();
        }

        public async Task InitializeAsync()
        {
            if (string.IsNullOrWhiteSpace(Email))
            {
                var lastEmail = await _sessionService.GetLastLoginEmailAsync();
                if (!string.IsNullOrWhiteSpace(lastEmail) && string.IsNullOrWhiteSpace(Email))
                {
                    Email = lastEmail;
                }
            }
        }

        #region Commands
        [RelayCommand]
        async Task LoginAsync()
        {
            if (IsBusy) return;

            IsBusy = true;
            ErrorMessage = null;

            try
            {
                var result = await _loginUseCase.ExecuteLoginAsync(Email, Password);

                if (result.OtpRequired)
                {
                    _pendingLoginStore.Set(Email, Password);
                    Password = string.Empty;
                    if (Shell.Current != null) await Shell.Current.GoToAsync("//OtpPage");
                }
                else if (result.IsSuccess)
                {
                    _pendingLoginStore.Clear();
                    Password = string.Empty;
                    if (AppShell.Current != null && result.User != null)
                    {
                        AppShell.Current.FlyoutHeader = new FlyoutHeaderControl(result.User);
                    }
                    if (Shell.Current != null) await Shell.Current.GoToAsync(Easymakemoney.Utilities.HomeRoute.For(result.User));
                }
                else
                {
                    ErrorMessage = result.ErrorMessage;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LoginPageViewModel] Erreur connexion : {ex.Message}");
                ErrorMessage = "Une erreur est survenue, réessayez.";
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        async Task ChangeSiteAsync()
        {
            if (IsBusy) return;
            await _authFlowService.PromptAndChangeSiteAsync();
        }
        #endregion
    }
}
