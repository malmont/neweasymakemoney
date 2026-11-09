namespace Easymakemoney.Components;

/// <summary>Une ligne libellé et montant ; <see cref="IsTotal"/> la met en valeur avec un filet au-dessus.</summary>
public partial class UiAmountRow : ContentView
{
    public static readonly BindableProperty TitleProperty =
        BindableProperty.Create(nameof(Title), typeof(string), typeof(UiAmountRow),
            propertyChanged: (bindable, _, value) => ((UiAmountRow)bindable).TitleLabel.Text = value as string);

    public static readonly BindableProperty ValueProperty =
        BindableProperty.Create(nameof(Value), typeof(string), typeof(UiAmountRow),
            propertyChanged: (bindable, _, value) => ((UiAmountRow)bindable).ValueLabel.Text = value as string);

    public static readonly BindableProperty IsTotalProperty =
        BindableProperty.Create(nameof(IsTotal), typeof(bool), typeof(UiAmountRow), false,
            propertyChanged: (bindable, _, value) => ((UiAmountRow)bindable).ApplyTotal((bool)value));

    public string? Title
    {
        get => (string?)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string? Value
    {
        get => (string?)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public bool IsTotal
    {
        get => (bool)GetValue(IsTotalProperty);
        set => SetValue(IsTotalProperty, value);
    }

    public UiAmountRow() => InitializeComponent();

    private void ApplyTotal(bool isTotal)
    {
        Separator.IsVisible = isTotal;
        TitleLabel.FontFamily = isTotal ? "InterBold" : "Inter";
        TitleLabel.TextColor = isTotal ? (Color)Application.Current!.Resources["UiTextPrimary"] : (Color)Application.Current!.Resources["UiTextSecondary"];
        ValueLabel.FontFamily = isTotal ? "InterBold" : "InterSemiBold";
    }
}
