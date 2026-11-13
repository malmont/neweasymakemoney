namespace Easymakemoney.Utilities
{
    /// <summary>
    /// Une liste d'un formulaire (types, catégories, transporteurs…) n'a pas pu être chargée : le gérant est prévenu, une seule fois
    /// même si plusieurs listes du même formulaire échouent ensemble, au lieu d'une liste vide sans explication.
    /// </summary>
    public static class FormLoadNotice
    {
        private static readonly TimeSpan Window = TimeSpan.FromSeconds(10);
        private static DateTime _lastShownUtc = DateTime.MinValue;
        private static readonly object Gate = new();

        public const string Message = "Certaines listes de ce formulaire n'ont pas pu être chargées. Vérifiez la connexion, puis rouvrez le formulaire.";

        /// <summary>Horloge et affichage (points d'entrée des tests).</summary>
        public static Func<DateTime> UtcNow { get; set; } = () => DateTime.UtcNow;
        public static Func<string, string, Task> ShowAsync { get; set; } = AppDialogs.InfoAsync;

        public static Task ReportAsync(string what, Exception ex)
        {
            Debug.WriteLine($"[FormLoadNotice] {what} : {ex.Message}");
            lock (Gate)
            {
                var now = UtcNow();
                if (now - _lastShownUtc < Window)
                    return Task.CompletedTask;
                _lastShownUtc = now;
            }
            return ShowAsync("Liste indisponible", Message);
        }

        public static void ResetForTests() => _lastShownUtc = DateTime.MinValue;
    }
}
