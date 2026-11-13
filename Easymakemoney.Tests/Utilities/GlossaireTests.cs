using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Easymakemoney.Utilities;
using Xunit;

namespace Easymakemoney.Tests.Utilities;

[Collection("Glossaire")]
public class GlossaireTests
{
    private static string AppDirectory([CallerFilePath] string path = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(path)!, "..", "..", "Easymakemoney"));

    [Fact]
    public void EveryTermUsedOnScreen_HasADefinition()
    {
        var files = Directory.GetFiles(Path.Combine(AppDirectory(), "Views"), "*.xaml", SearchOption.AllDirectories);
        var used = files
            .SelectMany(file => Regex.Matches(File.ReadAllText(file), "Term=\"([^\"{]+)\"").Select(match => (File: Path.GetFileName(file), Key: match.Groups[1].Value)))
            .ToList();

        Assert.NotEmpty(used);
        var missing = used.Where(u => Glossaire.Find(u.Key) == null).Select(u => $"{u.File} : {u.Key}").ToList();
        Assert.True(missing.Count == 0, "Termes sans définition : " + string.Join(", ", missing));
    }

    [Fact]
    public void Definitions_AreWrittenInFrenchAndNotEmpty()
    {
        Assert.All(Glossaire.Keys, key =>
        {
            var entry = Glossaire.Find(key)!;
            Assert.False(string.IsNullOrWhiteSpace(entry.Title));
            Assert.True(entry.Definition.Length > 20, key);
        });
    }
}
