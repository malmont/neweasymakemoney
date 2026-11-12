using Microsoft.Maui.Controls.Shapes;

namespace Easymakemoney.Components;

public enum UiDialogKind
{
    Info,
    Error,
    Confirm,
    /// <summary>Une saisie : un champ, « Valider » rend le texte, « Annuler » rend null.</summary>
    Prompt,
    /// <summary>Un choix dans une liste : un bouton par réponse, « Annuler » rend null.</summary>
    Choose,
}

/// <summary>Champ d'une saisie : valeur de départ, clavier, texte d'exemple.</summary>
public sealed record UiDialogInput(string? InitialValue = null, Keyboard? Keyboard = null, string? Placeholder = null);

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
        UiDialogKind.Prompt => "✎",
        UiDialogKind.Choose => "?",
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
    private readonly TaskCompletionSource<string?> _answer = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private Entry? _entry;
    private readonly Border _card;
    private readonly bool _closesOnOutsideTap;
    private bool _closing;

    public Task<bool> Result => _result.Task;

    /// <summary>Texte saisi (Prompt) ou réponse choisie (Choose) ; null si le gérant annule.</summary>
    public Task<string?> Answer => _answer.Task;

    public UiDialogView(UiDialogKind kind, string title, string message, string accept, string? cancel,
        UiDialogInput? input = null, IReadOnlyList<string>? choices = null)
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
            },
        };

        if (!string.IsNullOrWhiteSpace(message))
        {
            body.Children.Add(new ScrollView
            {
                MaximumHeightRequest = Math.Max(160, screenHeight * (kind == UiDialogKind.Choose ? 0.2 : 0.45)),
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
            });
        }

        if (kind == UiDialogKind.Prompt)
        {
            // Champ sans bordure native : la bordure arrondie est celle de la carte (comme les formulaires de l'appli).
            _entry = new Easymakemoney.Handlers.BorderlessEntry
            {
                Text = input?.InitialValue,
                Placeholder = input?.Placeholder,
                Keyboard = input?.Keyboard ?? Keyboard.Default,
                FontFamily = "Inter",
                FontSize = 16,
                TextColor = Resource("UiTextPrimary", "#0F172A"),
                PlaceholderColor = Resource("UiTextMuted", "#94A3B8"),
                BackgroundColor = Colors.Transparent,
                ReturnCommand = new Command(() => _ = CloseAsync(true, _entry?.Text ?? string.Empty)),
            };
            body.Children.Add(new Border
            {
                Margin = new Thickness(0, 16, 0, 0),
                Padding = new Thickness(12, 0),
                MinimumHeightRequest = 48,
                Stroke = Resource("UiBorder", "#E2E8F0"),
                StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = 12 },
                BackgroundColor = Resource("UiSurface", "#FFFFFF"),
                Content = _entry,
            });
        }

        if (kind == UiDialogKind.Choose)
        {
            // Un bouton par réponse, dans une liste qui défile si elle est longue.
            var list = new VerticalStackLayout { Spacing = 8 };
            foreach (var choice in choices ?? Array.Empty<string>())
            {
                var value = choice;
                list.Children.Add(new Button
                {
                    Text = value,
                    FontFamily = "InterSemiBold",
                    FontSize = 15,
                    MinimumHeightRequest = 48,
                    CornerRadius = (int)Math.Round(buttonRadius),
                    BackgroundColor = accentTint,
                    TextColor = primary,
                    LineBreakMode = LineBreakMode.WordWrap,
                    Command = new Command(() => _ = CloseAsync(true, value)),
                });
            }
            body.Children.Add(new ScrollView
            {
                MaximumHeightRequest = Math.Max(200, screenHeight * 0.45),
                Margin = new Thickness(0, 16, 0, 0),
                Content = list,
            });
        }
        else
        {
            body.Children.Add(new Button
            {
                Text = accept,
                FontFamily = "InterSemiBold",
                FontSize = 16,
                HeightRequest = 50,
                CornerRadius = (int)Math.Round(buttonRadius),
                BackgroundColor = accentColor,
                TextColor = destructive || kind == UiDialogKind.Error ? Colors.White : onPrimary,
                Margin = new Thickness(0, 20, 0, 0),
                Command = new Command(() => _ = CloseAsync(true, kind == UiDialogKind.Prompt ? _entry?.Text ?? string.Empty : null)),
            });
        }

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
        _entry?.Focus();
    }

    /// <summary>Touche retour d'Android : comme « Annuler » (ou « OK » pour une simple information).</summary>
    public void OnBack() => _ = CloseAsync(false);

    private async Task CloseAsync(bool accepted, string? answer = null)
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
        _entry?.Unfocus();
        _answer.TrySetResult(accepted ? answer : null);
        _result.TrySetResult(accepted);
    }

    private static Color Resource(string key, string fallback) =>
        Application.Current?.Resources.TryGetValue(key, out var value) == true && value is Color color
            ? color
            : Color.FromArgb(fallback);
}
