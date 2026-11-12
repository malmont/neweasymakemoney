using Easymakemoney.Services.Offline;

namespace Easymakemoney.Tests.Fakes;

/// <summary>Faux cache hors connexion en mémoire pour vérifier le vidage des réponses statistiques et trésorerie.</summary>
public class MemoryResponseCache : IResponseCache
{
    public Dictionary<string, (string Body, DateTime SavedAt)> Items { get; } = new();
    public int ClearCalls { get; private set; }

    public void Save(string key, string body, DateTime savedAt) => Items[key] = (body, savedAt);

    public (string Body, DateTime SavedAt)? Load(string key) => Items.TryGetValue(key, out var item) ? item : null;

    public void Clear()
    {
        ClearCalls++;
        Items.Clear();
    }
}

