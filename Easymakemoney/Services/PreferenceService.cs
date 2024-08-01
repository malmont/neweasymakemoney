
namespace Easymakemoney.Services
{
    public class PreferenceService : IPreferenceService
    {
        public void SaveUserDetails(string token, string username, string role)
        {
            string encryptedToken = EncryptionHelper.Encrypt(token);
            Microsoft.Maui.Storage.Preferences.Set("userToken", encryptedToken);

            UserBasicInfo userDetails = new UserBasicInfo
            {
                Email = username,
                Role = role
            };

            string userDetailStr = JsonConvert.SerializeObject(userDetails);
            Microsoft.Maui.Storage.Preferences.Set(nameof(App.UserDetails), userDetailStr);
            App.UserDetails = userDetails;
            App.Token = token;
        }

        public string GetUserToken()
        {
            var encryptedToken = Microsoft.Maui.Storage.Preferences.Get("userToken", string.Empty);
            return EncryptionHelper.Decrypt(encryptedToken);
        }
    }
}
