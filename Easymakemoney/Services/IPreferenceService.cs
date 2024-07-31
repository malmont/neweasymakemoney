namespace Easymakemoney.Services
{
    public interface IPreferenceService
    {
        void SaveUserDetails(string token, string username, string role);
    }
}

