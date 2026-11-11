using Easymakemoney.Models;
using Easymakemoney.Utilities;

namespace Easymakemoney.Components;

/// <summary>
/// Graphique des statistiques : barres pour la plage choisie, ligne grise pour l'an dernier aux mêmes dates.
/// Un appui (ou un glissé) sélectionne un point ; SelectedIndex remonte au ViewModel pour afficher sa valeur exacte.
/// </summary>
public class StatsChartView : GraphicsView, IDrawable
{
    public static readonly BindableProperty PointsProperty =
        BindableProperty.Create(nameof(Points), typeof(IList<StatsChartPoint>), typeof(StatsChartView), null, propertyChanged: Redraw);

    public static readonly BindableProperty IsMoneyProperty =
        BindableProperty.Create(nameof(IsMoney), typeof(bool), typeof(StatsChartView), true, propertyChanged: Redraw);

    public static readonly BindableProperty CurrencyProperty =
        BindableProperty.Create(nameof(Currency), typeof(string), typeof(StatsChartView), null, propertyChanged: Redraw);

    public static readonly BindableProperty SelectedIndexProperty =
        BindableProperty.Create(nameof(SelectedIndex), typeof(int), typeof(StatsChartView), -1, BindingMode.TwoWay, propertyChanged: Redraw);

    private const float Top = 22;
    private const float Bottom = 22;

    public IList<StatsChartPoint>? Points
    {
        get => (IList<StatsChartPoint>?)GetValue(PointsProperty);
        set => SetValue(PointsProperty, value);
    }

    public bool IsMoney
    {
        get => (bool)GetValue(IsMoneyProperty);
        set => SetValue(IsMoneyProperty, value);
    }

    public string? Currency
    {
        get => (string?)GetValue(CurrencyProperty);
        set => SetValue(CurrencyProperty, value);
    }

    public int SelectedIndex
    {
        get => (int)GetValue(SelectedIndexProperty);
        set => SetValue(SelectedIndexProperty, value);
    }

    public StatsChartView()
    {
        Drawable = this;
        HeightRequest = 200;
        StartInteraction += (_, e) => Select(e.Touches.FirstOrDefault().X);
        DragInteraction += (_, e) => Select(e.Touches.FirstOrDefault().X);
        SemanticProperties.SetDescription(this, "Graphique : touchez une barre pour lire sa valeur");
    }

    private static void Redraw(BindableObject bindable, object oldValue, object newValue) => ((StatsChartView)bindable).Invalidate();

    private void Select(float x)
    {
        var count = Points?.Count ?? 0;
        if (count == 0 || Width <= 0)
            return;
        var index = (int)Math.Floor(x / (Width / count));
        SelectedIndex = Math.Clamp(index, 0, count - 1);
    }

    public void Draw(ICanvas canvas, RectF rect)
    {
        var points = Points;
        var primary = Resource("ThemePrimary", "#512BD4");
        var muted = Resource("UiTextMuted", "#64748B");
        var border = Resource("UiBorder", "#E2E8F0");

        canvas.FontSize = 11;
        canvas.FontColor = muted;

        if (points == null || points.Count == 0 || points.All(p => p.Value == 0 && (p.LastYear ?? 0) == 0 && !p.IsEstimate))
        {
            canvas.DrawString("Aucune vente sur cette plage, ni l'an dernier aux mêmes dates", rect, HorizontalAlignment.Center, VerticalAlignment.Center);
            return;
        }

        var max = Math.Max(0, points.Select(p => Math.Max(p.Value, p.LastYear ?? 0)).DefaultIfEmpty(0).Max());
        var min = Math.Min(0, points.Select(p => Math.Min(p.Value, p.LastYear ?? 0)).DefaultIfEmpty(0).Min());
        if (max - min <= 0)
            max = 1;

        var plot = new RectF(rect.X, rect.Y + Top, rect.Width, rect.Height - Top - Bottom);
        var slot = plot.Width / points.Count;
        var barWidth = Math.Max(2f, Math.Min(slot * 0.62f, 26f));
        float Y(double value) => plot.Bottom - (float)((value - min) / (max - min)) * plot.Height;
        var zero = Y(0);

        // Repères : ligne du zéro, ligne du maximum avec sa valeur (et du minimum s'il est négatif).
        canvas.StrokeColor = border;
        canvas.StrokeSize = 1;
        canvas.DrawLine(plot.Left, zero, plot.Right, zero);
        canvas.StrokeDashPattern = new float[] { 3, 3 };
        if (max > 0)
            canvas.DrawLine(plot.Left, plot.Top, plot.Right, plot.Top);
        if (min < 0)
            canvas.DrawLine(plot.Left, plot.Bottom, plot.Right, plot.Bottom);
        canvas.StrokeDashPattern = null;
        if (max > 0)
            canvas.DrawString(Format(max), rect.X, rect.Y, rect.Width, Top - 4, HorizontalAlignment.Left, VerticalAlignment.Center);
        if (min < 0)
            canvas.DrawString(Format(min), rect.X, plot.Bottom - 16, rect.Width, 14, HorizontalAlignment.Right, VerticalAlignment.Center);

        var negative = Color.FromArgb("#B91C1C");
        var selected = SelectedIndex;
        for (var i = 0; i < points.Count; i++)
        {
            var value = points[i].Value;
            if (value == 0)
                continue;
            var centerX = plot.Left + slot * i + slot / 2;
            var y = Y(value);
            var color = value < 0 ? negative : primary;
            var alpha = selected < 0 || selected == i ? 1f : 0.35f;
            if (points[i].IsEstimate)
                alpha *= 0.4f; // estimé : plus clair
            canvas.FillColor = color.WithAlpha(alpha);
            var radius = Math.Min(4f, barWidth / 2);
            if (value > 0)
                canvas.FillRoundedRectangle(centerX - barWidth / 2, y, barWidth, zero - y, radius, radius, 0, 0);
            else
                canvas.FillRoundedRectangle(centerX - barWidth / 2, zero, barWidth, y - zero, 0, 0, radius, radius);
        }

        // An dernier : ligne grise reliant les mêmes dates.
        // Rien à comparer (aucune vente l'an dernier) : pas de ligne plate à zéro.
        if (points.Any(p => p.LastYear is { } last && last != 0))
        {
            var path = new PathF();
            var started = false;
            for (var i = 0; i < points.Count; i++)
            {
                if (points[i].LastYear is not { } last)
                    continue;
                var x = plot.Left + slot * i + slot / 2;
                var y = Y(last);
                if (!started)
                {
                    path.MoveTo(x, y);
                    started = true;
                }
                else
                    path.LineTo(x, y);
            }
            canvas.StrokeColor = muted.WithAlpha(0.8f);
            canvas.StrokeSize = 2;
            canvas.StrokeLineJoin = LineJoin.Round;
            canvas.DrawPath(path);
            if (points.Count <= 31)
            {
                canvas.FillColor = muted;
                for (var i = 0; i < points.Count; i++)
                    if (points[i].LastYear is { } last)
                        canvas.FillCircle(plot.Left + slot * i + slot / 2, Y(last), 2.5f);
            }
        }

        // Point sélectionné : trait vertical.
        if (selected >= 0 && selected < points.Count)
        {
            var x = plot.Left + slot * selected + slot / 2;
            canvas.StrokeColor = primary.WithAlpha(0.5f);
            canvas.StrokeSize = 1;
            canvas.DrawLine(x, plot.Top, x, plot.Bottom);
        }

        // Axe (sous la zone de tracé) : premier, milieu et dernier libellés.
        canvas.FontColor = muted;
        var labels = points.Count switch
        {
            1 => new[] { 0 },
            2 => new[] { 0, 1 },
            _ => new[] { 0, points.Count / 2, points.Count - 1 },
        };
        foreach (var i in labels)
        {
            var centerX = plot.Left + slot * i + slot / 2;
            var align = i == 0 ? HorizontalAlignment.Left : i == points.Count - 1 ? HorizontalAlignment.Right : HorizontalAlignment.Center;
            var x = align switch
            {
                HorizontalAlignment.Left => Math.Max(rect.Left, centerX - slot / 2),
                HorizontalAlignment.Right => Math.Min(rect.Right, centerX + slot / 2) - 90,
                _ => centerX - 45,
            };
            canvas.DrawString(points[i].ShortLabel, x, plot.Bottom + 4, 90, Bottom - 4, align, VerticalAlignment.Top);
        }
    }

    private string Format(double value) => IsMoney
        ? SiteCurrency.FormatCents(value, null, Currency)
        : Math.Round(value).ToString("N0");

    private static Color Resource(string key, string fallback) =>
        Application.Current?.Resources.TryGetValue(key, out var value) == true && value is Color color ? color : Color.FromArgb(fallback);
}

/// <summary>Petite courbe de tendance d'une carte d'indicateur.</summary>
public class SparklineView : GraphicsView, IDrawable
{
    public static readonly BindableProperty ValuesProperty =
        BindableProperty.Create(nameof(Values), typeof(IList<double>), typeof(SparklineView), null,
            propertyChanged: (bindable, _, _) => ((SparklineView)bindable).Invalidate());

    public IList<double>? Values
    {
        get => (IList<double>?)GetValue(ValuesProperty);
        set => SetValue(ValuesProperty, value);
    }

    public SparklineView()
    {
        Drawable = this;
        HeightRequest = 28;
        InputTransparent = true;
    }

    public void Draw(ICanvas canvas, RectF rect)
    {
        var values = Values;
        if (values == null || values.Count < 2)
            return;

        var min = values.Min();
        var max = values.Max();
        var span = max - min;
        var inset = 3f;
        float X(int i) => rect.Left + inset + (rect.Width - 2 * inset) * i / (values.Count - 1);
        float Y(double v) => span <= 0 ? rect.Center.Y : rect.Bottom - inset - (float)((v - min) / span) * (rect.Height - 2 * inset);

        var path = new PathF();
        path.MoveTo(X(0), Y(values[0]));
        for (var i = 1; i < values.Count; i++)
            path.LineTo(X(i), Y(values[i]));

        var primary = Application.Current?.Resources.TryGetValue("ThemePrimary", out var value) == true && value is Color color ? color : Color.FromArgb("#512BD4");
        canvas.StrokeColor = primary;
        canvas.StrokeSize = 2;
        canvas.StrokeLineJoin = LineJoin.Round;
        canvas.StrokeLineCap = LineCap.Round;
        canvas.DrawPath(path);
        canvas.FillColor = primary;
        canvas.FillCircle(X(values.Count - 1), Y(values[^1]), 2.5f);
    }
}
