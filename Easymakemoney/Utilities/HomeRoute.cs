using Easymakemoney.Models.Session;

namespace Easymakemoney.Utilities
{
    /// <summary>
    /// Page d'accueil selon le compte : l'accueil de gestion pour un administrateur, la boîte des demandes pour un compte
    /// Assistance seul, qui ne doit rien voir d'autre (décision du 11/11/2026).
    /// </summary>
    public static class HomeRoute
    {
        public const string SupportRouteName = "SupportHomePage";
        public const string Admin = "//DashboardPage";
        public const string Support = "//" + SupportRouteName;

        public static string For(UserInfo? user) => user?.IsSupportOnly == true ? Support : Admin;
    }
}
