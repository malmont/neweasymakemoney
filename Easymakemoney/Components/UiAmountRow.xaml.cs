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

    /// <summary>Clé du Glossaire : le titre devient un lien vers la définition du terme.</summary>
    public static readonly BindableProperty TermProperty =
        BindableProperty.Create(nameof(Term), typeof(string), typeof(UiAmountRow),
            propertyChanged: (bindable, _, value) => ((UiAmountRow)bindable).ApplyTerm(value as string));

    public string? Term
    {
        get => (string?)GetValue(TermProperty);
        set => SetValue(TermProperty, value);
    }

    private void ApplyTerm(string? term)
    {
        if (Utilities.Glossaire.Find(term) is null)
            return;
        TitleLabel.TextDecorations = TextDecorations.Underline;
        TitleLabel.SetDynamicResource(Label.TextColorProperty, "ThemePrimary");
        SemanticProperties.SetHint(TitleLabel, "Afficher la définition");
        TitleLabel.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(async () =>
            {
                if (Utilities.Glossaire.Find(Term) is not { } entry)
                    return;
                if (IPlatformApplication.Current?.Services.GetService<Services.Dialog.IDialogService>() is { } dialog)
                    await dialog.ShowInfoAsync(entry.Definition, entry.Title);
            }),
        });
    }

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
        // Un titre en lien garde la couleur du thème.
        if (Utilities.Glossaire.Find(Term) is null)
            TitleLabel.TextColor = isTotal ? (Color)Application.Current!.Resources["UiTextPrimary"] : (Color)Application.Current!.Resources["UiTextSecondary"];
        ValueLabel.FontFamily = isTotal ? "InterBold" : "InterSemiBold";
    }
}
