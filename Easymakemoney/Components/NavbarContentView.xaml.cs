
namespace Easymakemoney.Components
{
    public partial class NavbarContentView : ContentView
    {
        public static readonly BindableProperty ButtonTextProperty =
            BindableProperty.Create(nameof(ButtonText), typeof(string), typeof(NavbarContentView), default(string));

        public static readonly BindableProperty ButtonCommandProperty =
            BindableProperty.Create(nameof(ButtonCommand), typeof(ICommand), typeof(NavbarContentView));

        public static readonly BindableProperty ImageButtonSourceListProperty =
            BindableProperty.Create(nameof(ImageButtonSourceList), typeof(ImageSource), typeof(NavbarContentView));

        public static readonly BindableProperty ImageButtonSourceProperty =
            BindableProperty.Create(nameof(ImageButtonSource), typeof(ImageSource), typeof(NavbarContentView));

        public static readonly BindableProperty ImageButtonCommandProperty =
            BindableProperty.Create(nameof(ImageButtonCommand), typeof(ICommand), typeof(NavbarContentView));

        public static readonly BindableProperty ImageButtonCommandListProperty =
            BindableProperty.Create(nameof(ImageButtonCommandList), typeof(ICommand), typeof(NavbarContentView),
                propertyChanged: (bindable, _, value) => ((NavbarContentView)bindable).ThumbTapArea.IsVisible = value != null);

        public static readonly BindableProperty LabelTextProperty =
            BindableProperty.Create(nameof(LabelText), typeof(string), typeof(NavbarContentView), default(string));

        public static readonly BindableProperty ValueTextProperty =
            BindableProperty.Create(nameof(ValueText), typeof(string), typeof(NavbarContentView), default(string),
                propertyChanged: (bindable, _, _) => ((NavbarContentView)bindable).UpdateValueState());

        public static readonly BindableProperty PlaceholderTextProperty =
            BindableProperty.Create(nameof(PlaceholderText), typeof(string), typeof(NavbarContentView), default(string));

        public static readonly BindableProperty IconProperty =
            BindableProperty.Create(nameof(Icon), typeof(string), typeof(NavbarContentView), default(string));

        public static readonly BindableProperty HideCreateProperty =
            BindableProperty.Create(nameof(HideCreate), typeof(bool), typeof(NavbarContentView), false,
                propertyChanged: (bindable, _, value) => ((NavbarContentView)bindable).UpdateCreateVisibility((bool)value));

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

        public ImageSource ImageButtonSourceList
        {
            get => (ImageSource)GetValue(ImageButtonSourceListProperty);
            set => SetValue(ImageButtonSourceListProperty, value);
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

        public ICommand ImageButtonCommandList
        {
            get => (ICommand)GetValue(ImageButtonCommandListProperty);
            set => SetValue(ImageButtonCommandListProperty, value);
        }

        public string LabelText
        {
            get => (string)GetValue(LabelTextProperty);
            set => SetValue(LabelTextProperty, value);
        }

        public string ValueText
        {
            get => (string)GetValue(ValueTextProperty);
            set => SetValue(ValueTextProperty, value);
        }

        public string PlaceholderText
        {
            get => (string)GetValue(PlaceholderTextProperty);
            set => SetValue(PlaceholderTextProperty, value);
        }

        public string Icon
        {
            get => (string)GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        public bool HideCreate
        {
            get => (bool)GetValue(HideCreateProperty);
            set => SetValue(HideCreateProperty, value);
        }

        public NavbarContentView()
        {
            InitializeComponent();
            UpdateValueState();
        }

        private void UpdateValueState()
        {
            var hasValue = !string.IsNullOrWhiteSpace(ValueText);
            ValueLabel.IsVisible = hasValue;
            PlaceholderLabel.IsVisible = !hasValue;
        }

        private void UpdateCreateVisibility(bool hideCreate)
        {
            CreateButton.IsVisible = !hideCreate;
            CreateSpacer.IsVisible = !hideCreate;
        }

        // Sans élément choisi, le détail n'a rien à ouvrir : la vignette ouvre alors la liste, comme le reste de la carte.
        private void OnThumbTapped(object? sender, TappedEventArgs e)
        {
            var command = string.IsNullOrWhiteSpace(ValueText) ? ButtonCommand : ImageButtonCommandList;
            if (command?.CanExecute(null) == true)
            {
                command.Execute(null);
            }
        }
    }
}
