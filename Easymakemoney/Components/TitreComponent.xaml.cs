namespace Easymakemoney.Components
{
    public partial class TitreComponent : ContentView
    {
        public static readonly BindableProperty IconSourceProperty =
            BindableProperty.Create(nameof(IconSource), typeof(ImageSource), typeof(TitreComponent));

        public static readonly BindableProperty HeaderTextProperty =
            BindableProperty.Create(nameof(HeaderText), typeof(string), typeof(TitreComponent), default(string));

        public static readonly BindableProperty ValueProperty =
            BindableProperty.Create(nameof(Value), typeof(string), typeof(TitreComponent), default(string));

        public static readonly BindableProperty IsExpensesVisibleProperty =
    BindableProperty.Create(nameof(IsExpensesVisible), typeof(bool), typeof(BudgetOverviewView), true);
        public ImageSource IconSource
        {
            get => (ImageSource)GetValue(IconSourceProperty);
            set => SetValue(IconSourceProperty, value);
        }

        public string HeaderText
        {
            get => (string)GetValue(HeaderTextProperty);
            set => SetValue(HeaderTextProperty, value);
        }

        public string Value
        {
            get => (string)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

         public bool IsExpensesVisible
        {
            get => (bool)GetValue(IsExpensesVisibleProperty);
            set => SetValue(IsExpensesVisibleProperty, value);
        }


        public TitreComponent()
        {
            InitializeComponent();
        }
    }
}
