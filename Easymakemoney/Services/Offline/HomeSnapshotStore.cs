namespace Easymakemoney.Services.Offline;

/// <summary>Dernier Accueil reçu du serveur, montré tel quel quand il n'y a pas de réseau.</summary>
public class HomeSnapshot
{
    public DateTime SavedAt { get; set; }
    public List<HomeTile> Tiles { get; set; } = new();
    public DaySummary? Day { get; set; }
    public List<ReadinessStep> ReadinessSteps { get; set; } = new();
}

public interface IHomeSnapshotStore
{
    void Save(string scope, HomeSnapshot snapshot);
    HomeSnapshot? Load(string scope);
}

/// <summary>
/// Copie locale de l'Accueil, par site et par compte (la clé porte les deux), dans les préférences de l'appli.
/// Les préférences sont vidées au changement de site (SiteDataCleaner). Aucune donnée n'est écrite hors de l'appareil.
/// </summary>
public class HomeSnapshotStore : IHomeSnapshotStore
{
    private const string Prefix = "home-snapshot:";

    public void Save(string scope, HomeSnapshot snapshot)
    {
        try
        {
            Preferences.Default.Set(Prefix + scope, JsonConvert.SerializeObject(snapshot));
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[HomeSnapshotStore] Copie non enregistrée : {ex.Message}");
        }
    }

    public HomeSnapshot? Load(string scope)
    {
        try
        {
            var json = Preferences.Default.Get(Prefix + scope, string.Empty);
            return string.IsNullOrEmpty(json) ? null : JsonConvert.DeserializeObject<HomeSnapshot>(json);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[HomeSnapshotStore] Copie illisible : {ex.Message}");
            return null;
        }
    }
}
