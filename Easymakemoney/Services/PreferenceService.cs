namespace Easymakemoney.Services
{
    public class PreferenceService : IPreferenceService
    {
        public void SaveUserDetails(string token, string username, string role, int userId)
        {
            string encryptedToken = EncryptionHelper.Encrypt(token);
            Microsoft.Maui.Storage.Preferences.Set("userToken", encryptedToken);
            Microsoft.Maui.Storage.Preferences.Set("userId", userId);
            Microsoft.Maui.Storage.Preferences.Set("userEmail", username);
             Microsoft.Maui.Storage.Preferences.Set("userRole", role); // Sauvegarder le rôle de l'utilisateur


            UserBasicInfo userDetails = new UserBasicInfo
            {
                Email = username,
                Role = role,
                UserId = userId
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

        public int GetUserId()
        {
            return Microsoft.Maui.Storage.Preferences.Get("userId", 0);
        }

        public string GetUserEmail()
        {
            return Microsoft.Maui.Storage.Preferences.Get("userEmail", string.Empty);
        }

         public string GetUserRole()
        {
            return Microsoft.Maui.Storage.Preferences.Get("userRole", string.Empty); // Récupérer le rôle de l'utilisateur
        }
    }
}
