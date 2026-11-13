using Easymakemoney.Services;
using Easymakemoney.Services.Dialog;
using Easymakemoney.Utilities;

namespace Easymakemoney.Components;

/// <summary>
/// La fenêtre d'un terme technique : la définition du lexique, puis, quand le serveur la rend, l'illustration avec les chiffres du site
/// (« Votre taux de marque ce mois-ci : 44 %… »). L'illustration est attendue 3 secondes au plus : au-delà, la définition seule.
/// </summary>
public static class TermDefinition
{
    public static readonly TimeSpan IllustrationTimeout = TimeSpan.FromSeconds(3);

    /// <summary>Le texte affiché : la définition, et l'illustration à la suite si elle existe.</summary>
    public static string Text(string definition, string? illustration) =>
        string.IsNullOrWhiteSpace(illustration) ? definition : $"{definition}\n\nChez vous : {illustration}";

    public static async Task ShowAsync(string? key)
    {
        if (Glossaire.Find(key) is not { } entry)
            return;
        var services = IPlatformApplication.Current?.Services;
        if (services?.GetService<IDialogService>() is not { } dialog)
            return;

        string? illustration = null;
        if (services.GetService<ILexiconService>() is { } lexicon)
        {
            var lookup = lexicon.GetIllustrationAsync(key!);
            if (await Task.WhenAny(lookup, Task.Delay(IllustrationTimeout)) == lookup)
                illustration = await lookup;
        }
        await dialog.ShowInfoAsync(Text(entry.Definition, illustration), entry.Title);
    }
}
