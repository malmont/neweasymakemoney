using Microsoft.Maui.Controls.Shapes;

namespace Easymakemoney.Components;

/// <summary>Vignette arrondie : l'icône reste visible tant qu'aucune image ne la recouvre.</summary>
public partial class UiThumb : ContentView
{
    public static readonly BindableProperty SourceProperty =
        BindableProperty.Create(nameof(Source), typeof(object), typeof(UiThumb),
            propertyChanged: (bindable, _, value) => ((UiThumb)bindable).ApplySource(value));

    public static readonly BindableProperty IconProperty =
        BindableProperty.Create(nameof(Icon), typeof(string), typeof(UiThumb),
            propertyChanged: (bindable, _, value) => ((UiThumb)bindable).ApplyIcon(value as string));

    /// <summary>Image à afficher : un <see cref="ImageSource"/> ou une chaîne (URL ou fichier).</summary>
    public object? Source
    {
        get => GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    /// <summary>Tracé de l'icône, au format SVG, dans un cadre de 24 x 24.</summary>
    public string? Icon
    {
        get => (string?)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public UiThumb() => InitializeComponent();

    private void ApplySource(object? value) => ThumbImage.Source = value switch
    {
        ImageSource source when !source.IsEmpty => source,
        string text when !string.IsNullOrWhiteSpace(text) => (ImageSource)text,
        _ => null,
    };

    private void ApplyIcon(string? value) =>
        IconPath.Data = string.IsNullOrWhiteSpace(value)
            ? null
            : (Geometry?)new PathGeometryConverter().ConvertFromInvariantString(value);
}
