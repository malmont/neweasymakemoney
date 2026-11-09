using Easymakemoney.Services.Auth;

namespace Easymakemoney.ViewModels
{
	public partial class AppShellViewModel:BaseViewModel
	{
		private readonly IAuthFlowService _authFlowService;

		public AppShellViewModel(IAuthFlowService authFlowService)
		{
			_authFlowService = authFlowService;
		}

		[RelayCommand]
		async Task ChangeSiteAsync()
		{
			await _authFlowService.PromptAndChangeSiteAsync();
		}

		[RelayCommand]
		async void SignOut()
		{
			await _authFlowService.LogoutAsync();
		}
	}
}
