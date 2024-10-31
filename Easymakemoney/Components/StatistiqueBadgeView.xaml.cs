

namespace Easymakemoney.Components
{
    public partial class StatistiqueBadgeView : ContentView
    {

        public static readonly BindableProperty ButtonStatistiqueProperty =
            BindableProperty.Create(nameof(ButtonStatistique), typeof(ICommand), typeof(StatistiqueBadgeView));


        public static readonly BindableProperty SourceImageProperty =
            BindableProperty.Create(nameof(SourceImage), typeof(string), typeof(StatistiqueBadgeView), default(string));

        public static readonly BindableProperty NameProperty =
            BindableProperty.Create(nameof(Name), typeof(string), typeof(StatistiqueBadgeView), default(string));

        public string SourceImage
        {
            get => (string)GetValue(SourceImageProperty);
            set => SetValue(SourceImageProperty, value);
        }

        public string Name
        {
            get => (string)GetValue(NameProperty);
            set => SetValue(NameProperty, value);
        }

        public ICommand ButtonStatistique
        {
            get => (ICommand)GetValue(ButtonStatistiqueProperty);
            set => SetValue(ButtonStatistiqueProperty, value);
        }

        public StatistiqueBadgeView()
        {
            InitializeComponent();
        }
    }
}
