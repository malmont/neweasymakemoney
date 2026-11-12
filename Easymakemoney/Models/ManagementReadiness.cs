using Newtonsoft.Json.Linq;

namespace Easymakemoney.Models
{
    /// <summary>
    /// Lot CHE : cheminement de l'application de gestion, groupe « app.management » de GET /api/site/readiness.
    /// Réponse : { percent, steps: [{ key, group, title, description, status: done|todo|optional, blocking, appScreen?, figures? }], groups }.
    /// L'appli affiche les textes du serveur tels quels ; elle ne compte que les étapes de son groupe.
    /// </summary>
    public class SiteReadiness
    {
        public const string ManagementGroup = "app.management";

        public List<ReadinessStep> steps { get; set; } = new();

        public List<ReadinessStep> ManagementSteps =>
            steps.Where(step => step.group == ManagementGroup).ToList();
    }

    public class ReadinessStep
    {
        public string key { get; set; } = string.Empty;
        public string group { get; set; } = string.Empty;
        public string? title { get; set; }
        public string? description { get; set; }

        /// <summary>Champs demandés au contrat CHE ; repli sur title et description.</summary>
        public string? label { get; set; }
        public string? hint { get; set; }

        public string status { get; set; } = "todo";
        public bool blocking { get; set; }
        public string? appScreen { get; set; }
        public JToken? figures { get; set; }

        public string Title => label ?? title ?? key;
        public string Text => hint ?? description ?? string.Empty;
        public bool HasText => !string.IsNullOrWhiteSpace(Text);

        public bool IsDone => status is "done" or "ok";
        public bool IsBlockingTodo => !IsDone && blocking && status != "optional";
        public bool IsImprovementTodo => !IsDone && !IsBlockingTodo;

        /// <summary>Écran de l'appli pour cette étape ; null = à faire sur le site.</summary>
        public string? Route => ReadinessRoutes.For(appScreen);
        public bool CanOpen => !IsDone && Route != null;
        public bool IsOnSite => !IsDone && Route == null;

        public string ActionText => Route != null ? "Ouvrir →" : appScreen == "notifications"
            ? "Bientôt dans l'application"
            : "À faire sur le site (administration)";

        public string StateText => IsDone ? "Fait" : IsBlockingTodo ? "À faire" : "Conseillé";
    }

    /// <summary>Codes d'écran du contrat CHE (même vocabulaire que les vignettes) vers les pages de l'appli.</summary>
    public static class ReadinessRoutes
    {
        public static string? For(string? appScreen) => appScreen switch
        {
            "periodes" => "ListNewCollectionPage",
            "exercice" => "FiscalYearPage",
            // Articles sans coût d'achat : ceux qui ne sont dans aucune commande (« À classer ») ; les rattacher leur donne un coût.
            "articles" => "AttachProductsPage",
            "fournisseurs" => "ListNewFournisseurPage",
            "tresorerie" => "TreasuryPage",
            "frais-fixes" => "FixedCostsPage",
            // Première commande fournisseur : la liste des commandes demande une période, on ouvre donc les périodes.
            "commandes" => "ListNewCollectionPage",
            _ => null,
        };
    }

    /// <summary>Résumé du groupe de gestion : ce qui bloque, ce qui est conseillé, ce qui est fait.</summary>
    public class ManagementReadinessSummary
    {
        public ManagementReadinessSummary(IReadOnlyList<ReadinessStep> steps)
        {
            Blocking = steps.Where(step => step.IsBlockingTodo).ToList();
            Improvements = steps.Where(step => step.IsImprovementTodo).ToList();
            Done = steps.Where(step => step.IsDone).ToList();
            Total = steps.Count;
        }

        public List<ReadinessStep> Blocking { get; }
        public List<ReadinessStep> Improvements { get; }
        public List<ReadinessStep> Done { get; }
        public int Total { get; }

        public int Percent => Total == 0 ? 100 : (int)Math.Round(Done.Count * 100.0 / Total);
        public bool IsReady => Blocking.Count == 0;

        public string HeadlineText => Total == 0
            ? "Aucune étape à afficher pour l'instant."
            : Blocking.Count > 0
                ? $"{Blocking.Count} étape{(Blocking.Count > 1 ? "s" : "")} à faire avant que les chiffres soient justes."
                : Improvements.Count > 0
                    ? $"L'essentiel est prêt. {Improvements.Count} réglage{(Improvements.Count > 1 ? "s" : "")} conseillé{(Improvements.Count > 1 ? "s" : "")} pour des chiffres plus complets."
                    : "Tout est prêt.";

        public string ProgressText => $"{Done.Count} sur {Total} · {Percent} %";
    }
}
