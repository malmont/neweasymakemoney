using System.Windows.Input;
using Microsoft.Maui.Controls.Shapes;

namespace Easymakemoney.Components;

public partial class UiTile : ContentView
{
    public static readonly BindableProperty CommandProperty =
        BindableProperty.Create(nameof(Command), typeof(ICommand), typeof(UiTile));

    public ICommand? Command
    {
        get => (ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public string Title
    {
        get => TitleLabel.Text;
        set { TitleLabel.Text = value; SemanticProperties.SetDescription(Card, value); }
    }

    public string Text
    {
        get => TextLabel.Text;
        set => TextLabel.Text = value;
    }

    public string Icon
    {
        set => IconPath.Data = (Geometry?)new PathGeometryConverter().ConvertFromInvariantString(value);
    }

    public UiTile() => InitializeComponent();

    private void OnTapped(object? sender, TappedEventArgs e)
    {
        if (Command?.CanExecute(null) == true)
        {
            Command.Execute(null);
        }
    }
}
