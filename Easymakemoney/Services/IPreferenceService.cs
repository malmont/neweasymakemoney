namespace Easymakemoney.Services
{
  public interface IPreferenceService
{
    void SaveUserDetails(string token, string username, string role, int userId);
    string GetUserToken();
    int GetUserId();
    string GetUserEmail();
    string GetUserRole();
}
}

