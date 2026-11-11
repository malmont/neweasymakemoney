using Microsoft.Maui.Controls.Shapes;

namespace Easymakemoney.Components;

public enum UiDialogKind
{
    Info,
    Error,
    Confirm,
}

/// <summary>Règles d'apparence de la boîte de dialogue, sans dépendance à l'écran (testables).</summary>
public static class UiDialogStyle
{
    private static readonly string[] DestructiveVerbs = { "supprimer", "effacer", "retirer" };

    /// <summary>Une confirmation qui détruit quelque chose a son bouton en rouge.</summary>
    public static bool IsDestructive(string? accept) =>
        !string.IsNullOrWhiteSpace(accept)
        && DestructiveVerbs.Any(verb => accept.TrimStart().StartsWith(verb, StringComparison.OrdinalIgnoreCase));

    /// <summary>Un message court est centré ; une liste ou un long texte reste aligné à gauche, plus lisible.</summary>
    public static bool CenterMessage(string? message) =>
        message is { Length: <= 160 } && !message.Contains('\n');

    public static string Glyph(UiDialogKind kind, bool destructive) => kind switch
    {
        UiDialogKind.Error => "!",
        UiDialogKind.Confirm => destructive ? "!" : "?",
        _ => "i",
    };
}

/// <summary>
/// Boîte de dialogue au style de l'application : carte arrondie, polices Inter, couleurs du thème du site.
/// Elle est posée en surimpression au-dessus de l'écran (voir DialogOverlay), sans navigation :
/// la page en dessous ne repasse pas par OnAppearing et ne recharge rien à la fermeture.
/// </summary>
public sealed class UiDialogView : Grid
{
    private const string DangerColor = "#B91C1C";
    private const string DangerTint = "#FEE2E2";

    private readonly TaskCompletionSource<bool> _result = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly Border _card;
    private readonly bool _closesOnOutsideTap;
    private bool _closing;

    public Task<bool> Result => _result.Task;

    public UiDialogView(UiDialogKind kind, string title, string message, string accept, string? cancel)
    {
        var destructive = kind == UiDialogKind.Confirm && UiDialogStyle.IsDestructive(accept);
        _closesOnOutsideTap = cancel == null;

        var primary = Resource("ThemePrimary", "#512BD4");
        var onPrimary = Resource("ThemeOnPrimary", "#FFFFFF");
        var accentColor = destructive || kind == UiDialogKind.Error ? Color.FromArgb(DangerColor) : primary;
        var accentTint = destructive || kind == UiDialogKind.Error ? Color.FromArgb(DangerTint) : Resource("ThemePrimaryTint", "#18512BD4");
        var buttonRadius = Application.Current?.Resources.TryGetValue("ThemeButtonRadius", out var radius) == true && radius is double r ? r : 14d;

        BackgroundColor = Color.FromArgb("#730F172A");
        Opacity = 0;

        // Fond : un appui à côté ferme une simple information, jamais une question.
        var backdrop = new BoxView { Color = Colors.Transparent };
        var outside = new TapGestureRecognizer();
        outside.Tapped += (_, _) =>
        {
            if (_closesOnOutsideTap)
                _ = CloseAsync(false);
        };
        backdrop.GestureRecognizers.Add(outside);
        Children.Add(backdrop);

        var badge = new Border
        {
            WidthRequest = 48,
            HeightRequest = 48,
            StrokeThickness = 0,
            BackgroundColor = accentTint,
            StrokeShape = new RoundRectangle { CornerRadius = 24 },
            HorizontalOptions = LayoutOptions.Center,
            Content = new Label
            {
                Text = UiDialogStyle.Glyph(kind, destructive),
                FontFamily = "InterBold",
                FontSize = 22,
                TextColor = accentColor,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
            },
        };

        var center = UiDialogStyle.CenterMessage(message);
        var display = DeviceDisplay.Current.MainDisplayInfo;
        var screenWidth = display.Density > 0 ? display.Width / display.Density : 390;
        var screenHeight = display.Density > 0 ? display.Height / display.Density : 800;

        var body = new VerticalStackLayout
        {
            Spacing = 0,
            Children =
            {
                badge,
                new Label
                {
                    Text = title,
                    FontFamily = "InterBold",
                    FontSize = 18,
                    TextColor = Resource("UiTextPrimary", "#0F172A"),
                    HorizontalTextAlignment = TextAlignment.Center,
                    Margin = new Thickness(0, 14, 0, 0),
                },
                new ScrollView
                {
                    MaximumHeightRequest = Math.Max(160, screenHeight * 0.45),
                    Margin = new Thickness(0, 8, 0, 0),
                    Content = new Label
                    {
                        Text = message,
                        FontFamily = "Inter",
                        FontSize = 15,
                        LineHeight = 1.3,
                        TextColor = Resource("UiTextSecondary", "#475569"),
                        HorizontalTextAlignment = center ? TextAlignment.Center : TextAlignment.Start,
                    },
                },
                new Button
                {
                    Text = accept,
                    FontFamily = "InterSemiBold",
                    FontSize = 16,
                    HeightRequest = 50,
                    CornerRadius = (int)Math.Round(buttonRadius),
                    BackgroundColor = accentColor,
                    TextColor = destructive || kind == UiDialogKind.Error ? Colors.White : onPrimary,
                    Margin = new Thickness(0, 20, 0, 0),
                    Command = new Command(() => _ = CloseAsync(true)),
                },
            },
        };

        if (cancel != null)
        {
            body.Children.Add(new Button
            {
                Text = cancel,
                FontFamily = "InterMedium",
                FontSize = 15,
                HeightRequest = 46,
                BackgroundColor = Colors.Transparent,
                TextColor = Resource("UiTextSecondary", "#475569"),
                Margin = new Thickness(0, 4, 0, 0),
                Command = new Command(() => _ = CloseAsync(false)),
            });
        }

        _card = new Border
        {
            BackgroundColor = Resource("UiSurface", "#FFFFFF"),
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 24 },
            Padding = new Thickness(24, 24, 24, cancel != null ? 12 : 24),
            WidthRequest = Math.Min(360, screenWidth - 48),
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            Shadow = new Shadow { Brush = Colors.Black, Opacity = 0.18f, Radius = 24, Offset = new Point(0, 8) },
            Scale = 0.94,
            Content = body,
        };
        // Un appui sur la carte ne doit pas traverser jusqu'au fond (Android passe le toucher aux vues en dessous).
        _card.GestureRecognizers.Add(new TapGestureRecognizer());
        Children.Add(_card);
    }

    public async Task AppearAsync()
    {
        await Task.WhenAll(
            this.FadeToAsync(1, 160, Easing.CubicOut),
            _card.ScaleToAsync(1, 160, Easing.CubicOut));
    }

    /// <summary>Touche retour d'Android : comme « Annuler » (ou « OK » pour une simple information).</summary>
    public void OnBack() => _ = CloseAsync(false);

    private async Task CloseAsync(bool accepted)
    {
        if (_closing)
            return;
        _closing = true;
        try
        {
            await this.FadeToAsync(0, 120, Easing.CubicIn);
        }
        catch
        {
            // L'animation est un confort : la réponse compte.
        }
        _result.TrySetResult(accepted);
    }

    private static Color Resource(string key, string fallback) =>
        Application.Current?.Resources.TryGetValue(key, out var value) == true && value is Color color
            ? color
            : Color.FromArgb(fallback);
}
