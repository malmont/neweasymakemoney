namespace Easymakemoney.Components
{
    public partial class StatistiqueDataBadgeView : ContentView
    {
        public static readonly BindableProperty DateProperty =
            BindableProperty.Create(nameof(Date), typeof(string), typeof(StatistiqueDataBadgeView), default(string));

        public static readonly BindableProperty RevenueProperty =
            BindableProperty.Create(nameof(Revenue), typeof(string), typeof(StatistiqueDataBadgeView), default(string));

        public static readonly BindableProperty FrameBackgroundColorProperty =
            BindableProperty.Create(nameof(FrameBackgroundColor), typeof(Color), typeof(StatistiqueDataBadgeView), Colors.Transparent);

        public string Date
        {
            get => (string)GetValue(DateProperty);
            set => SetValue(DateProperty, value);
        }

        public string Revenue
        {
            get => (string)GetValue(RevenueProperty);
            set => SetValue(RevenueProperty, value);
        }

        public Color FrameBackgroundColor
        {
            get => (Color)GetValue(FrameBackgroundColorProperty);
            set => SetValue(FrameBackgroundColorProperty, value);
        }

        public StatistiqueDataBadgeView()
        {
            InitializeComponent();
        }
    }
}
