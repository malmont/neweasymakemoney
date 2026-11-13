using Easymakemoney.Services.Dialog;
using Easymakemoney.Utilities;

namespace Easymakemoney.Components;

/// <summary>
/// Terme technique en lien : souligné, couleur du thème ; un appui affiche sa définition tirée du Glossaire.
/// Text est facultatif : sans lui, le titre du lexique est affiché.
/// </summary>
public class UiTerm : Label
{
    public static readonly BindableProperty TermProperty =
        BindableProperty.Create(nameof(Term), typeof(string), typeof(UiTerm), null,
            propertyChanged: (bindable, _, _) => ((UiTerm)bindable).ApplyTerm());

    public string? Term
    {
        get => (string?)GetValue(TermProperty);
        set => SetValue(TermProperty, value);
    }

    public UiTerm()
    {
        TextDecorations = TextDecorations.Underline;
        FontFamily = "InterMedium";
        SetDynamicResource(TextColorProperty, "ThemePrimary");
        SemanticProperties.SetHint(this, "Afficher la définition");
        GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(async () => await ShowAsync()) });
    }

    private void ApplyTerm()
    {
        if (string.IsNullOrEmpty(Text) && Glossaire.Find(Term) is { } entry)
            Text = entry.Title;
    }

    private Task ShowAsync() => TermDefinition.ShowAsync(Term);
}
