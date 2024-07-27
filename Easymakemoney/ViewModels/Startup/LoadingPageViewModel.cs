
namespace Easymakemoney.ViewModels.Startup
{
    public class LoadingPageViewModel
    {
        public LoadingPageViewModel()
        {
            checkUserLoginDetails();
        }

        private async void checkUserLoginDetails()
        {
            string userDetailsStr = Microsoft.Maui.Storage.Preferences.Get(nameof(App.UserDetails), "");

            if (string.IsNullOrWhiteSpace(userDetailsStr))
            {
                await Shell.Current.GoToAsync("//LoginPage");
            }
            else
            {
                var encryptedToken = Microsoft.Maui.Storage.Preferences.Get("userToken", string.Empty);
                var tokenDetails = EncryptionHelper.Decrypt(encryptedToken);

                var handler = new JwtSecurityTokenHandler();
                var jsonToken = handler.ReadJwtToken(tokenDetails) as JwtSecurityToken;

                if (jsonToken.ValidTo < DateTime.Now)
                {
                    await Shell.Current.DisplayAlert("User session expired", "Login again to continue", "OK");
                    await Shell.Current.GoToAsync("//LoginPage");
                }
                else
                {
                    var userInfo = JsonConvert.DeserializeObject<UserBasicInfo>(userDetailsStr);
                    App.UserDetails = userInfo;
                    App.Token = tokenDetails;
                    AppShell.Current.FlyoutHeader = new FlyoutHeaderControl();
                    await Shell.Current.GoToAsync("//DashboardPage");
                }
            }
        }
    }
}
