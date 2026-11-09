using Microsoft.Maui.Controls.Shapes;

namespace Easymakemoney.Components;

/// <summary>Carte dont le rayon suit le thème du site (ThemeCardRadius).</summary>
public class UiCard : Border
{
    public static readonly BindableProperty RadiusProperty =
        BindableProperty.Create(nameof(Radius), typeof(double), typeof(UiCard), 16d,
            propertyChanged: (bindable, _, value) => ((UiCard)bindable).ApplyRadius((double)value));

    public double Radius
    {
        get => (double)GetValue(RadiusProperty);
        set => SetValue(RadiusProperty, value);
    }

    public UiCard()
    {
        ApplyRadius(Radius);
        SetDynamicResource(RadiusProperty, "ThemeCardRadius");
    }

    private void ApplyRadius(double radius) => StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(radius) };
}
