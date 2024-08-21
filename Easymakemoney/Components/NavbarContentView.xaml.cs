
namespace Easymakemoney.Components
{
    public partial class NavbarContentView : ContentView
    {
        public static readonly BindableProperty ButtonTextProperty =
            BindableProperty.Create(nameof(ButtonText), typeof(string), typeof(NavbarContentView), default(string));

        public static readonly BindableProperty ButtonCommandProperty =
            BindableProperty.Create(nameof(ButtonCommand), typeof(ICommand), typeof(NavbarContentView));

        public static readonly BindableProperty ImageSourceProperty =
            BindableProperty.Create(nameof(ImageSource), typeof(ImageSource), typeof(NavbarContentView));

        public static readonly BindableProperty ImageButtonSourceProperty =
            BindableProperty.Create(nameof(ImageButtonSource), typeof(ImageSource), typeof(NavbarContentView));

        public static readonly BindableProperty ImageButtonCommandProperty =
            BindableProperty.Create(nameof(ImageButtonCommand), typeof(ICommand), typeof(NavbarContentView));

        public static readonly BindableProperty LabelTextProperty =
            BindableProperty.Create(nameof(LabelText), typeof(string), typeof(NavbarContentView), default(string));

        public string ButtonText
        {
            get => (string)GetValue(ButtonTextProperty);
            set => SetValue(ButtonTextProperty, value);
        }

        public ICommand ButtonCommand
        {
            get => (ICommand)GetValue(ButtonCommandProperty);
            set => SetValue(ButtonCommandProperty, value);
        }

        public ImageSource ImageSource
        {
            get => (ImageSource)GetValue(ImageSourceProperty);
            set => SetValue(ImageSourceProperty, value);
        }

        public ImageSource ImageButtonSource
        {
            get => (ImageSource)GetValue(ImageButtonSourceProperty);
            set => SetValue(ImageButtonSourceProperty, value);
        }

        public ICommand ImageButtonCommand
        {
            get => (ICommand)GetValue(ImageButtonCommandProperty);
            set => SetValue(ImageButtonCommandProperty, value);
        }

        public string LabelText
        {
            get => (string)GetValue(LabelTextProperty);
            set => SetValue(LabelTextProperty, value);
        }

        public NavbarContentView()
        {
            InitializeComponent();
        }
    }
}
