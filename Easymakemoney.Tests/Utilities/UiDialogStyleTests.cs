using Easymakemoney.Components;
using Xunit;

namespace Easymakemoney.Tests.Utilities;

public class UiDialogStyleTests
{
    [Theory]
    [InlineData("Supprimer", true)]
    [InlineData("  supprimer la ligne", true)]
    [InlineData("Effacer", true)]
    [InlineData("Retirer du voyage", true)]
    [InlineData("Oui", false)]
    [InlineData("Valider", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsDestructive_OnlyForDeletingVerbs(string? accept, bool expected) =>
        Assert.Equal(expected, UiDialogStyle.IsDestructive(accept));

    [Fact]
    public void CenterMessage_ShortSingleLineOnly()
    {
        Assert.True(UiDialogStyle.CenterMessage("Un solde négatif : le compte est passé sous zéro."));
        Assert.False(UiDialogStyle.CenterMessage("Champs invalides\n• amount: doit être positif"));
        Assert.False(UiDialogStyle.CenterMessage(new string('a', 161)));
        Assert.False(UiDialogStyle.CenterMessage(null));
    }

    [Fact]
    public void Glyph_FollowsKind()
    {
        Assert.Equal("i", UiDialogStyle.Glyph(UiDialogKind.Info, false));
        Assert.Equal("!", UiDialogStyle.Glyph(UiDialogKind.Error, false));
        Assert.Equal("?", UiDialogStyle.Glyph(UiDialogKind.Confirm, false));
        Assert.Equal("!", UiDialogStyle.Glyph(UiDialogKind.Confirm, true));
    }
}
