namespace Easymakemoney.Utilities
{
    /// <summary>
    /// Règle de l'App Store (3.1.1, hors des États-Unis) : sur iPhone, l'application ne propose ni essai, ni demande d'activation,
    /// ni achat, et ne renvoie pas vers le web pour en faire un. Elle dit seulement que le module n'est pas actif.
    /// </summary>
    public static class AppStorePolicy
    {
        /// <summary>Plateforme de l'appareil (remplacée dans les tests, où DeviceInfo n'existe pas).</summary>
        public static Func<bool> IsIos { get; set; } = () =>
        {
            try
            {
                var platform = DeviceInfo.Current.Platform;
                return platform == DevicePlatform.iOS || platform == DevicePlatform.MacCatalyst;
            }
            catch (Exception)
            {
                return false;
            }
        };

        /// <summary>Faux sur iPhone : pas de bouton « Essayer » ni « Demander », pas de renvoi vers le web.</summary>
        public static bool CanOfferModules => !IsIos();

        /// <summary>Texte d'un module absent : sur iPhone, le constat seul ; ailleurs, le constat et la façon de l'obtenir.</summary>
        public static string ModuleMissing(string missing, string howToGetIt) =>
            CanOfferModules ? $"{missing} {howToGetIt}" : missing;

        public static string LossModuleMissingText => ModuleMissing(
            "Ce site n'a pas encore la Prévention des pertes.",
            "Elle s'essaie gratuitement ou se demande sur le web, dans la gestion, thème « Prévention des pertes ».");

        public static string SupportModuleMissingText => ModuleMissing(
            "Ce site n'a pas le module Assistance.",
            "Il se demande ou s'active sur le site, dans « Mon site ».");
    }
}
