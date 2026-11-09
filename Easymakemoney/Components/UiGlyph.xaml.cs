using Microsoft.Maui.Controls.Shapes;

namespace Easymakemoney.Components;

/// <summary>Icône au trait, à la couleur du site, dans un cadre de 24 x 24.</summary>
public partial class UiGlyph : ContentView
{
    public static readonly BindableProperty DataProperty =
        BindableProperty.Create(nameof(Data), typeof(string), typeof(UiGlyph),
            propertyChanged: (bindable, _, value) => ((UiGlyph)bindable).Apply(value as string));

    /// <summary>Tracé de l'icône, au format SVG.</summary>
    public string? Data
    {
        get => (string?)GetValue(DataProperty);
        set => SetValue(DataProperty, value);
    }

    public UiGlyph() => InitializeComponent();

    private void Apply(string? value) =>
        GlyphPath.Data = string.IsNullOrWhiteSpace(value)
            ? null
            : (Geometry?)new PathGeometryConverter().ConvertFromInvariantString(value);
}
