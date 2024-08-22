namespace Easymakemoney.Components
{
    public partial class BudgetOverviewView : ContentView
    {
        public static readonly BindableProperty IconSourceProperty =
            BindableProperty.Create(nameof(IconSource), typeof(ImageSource), typeof(BudgetOverviewView));

        public static readonly BindableProperty HeaderTextProperty =
            BindableProperty.Create(nameof(HeaderText), typeof(string), typeof(BudgetOverviewView), default(string));

        public static readonly BindableProperty TotalLabelTextProperty =
            BindableProperty.Create(nameof(TotalLabelText), typeof(string), typeof(BudgetOverviewView), default(string));

        public static readonly BindableProperty TotalValueProperty =
            BindableProperty.Create(nameof(TotalValue), typeof(string), typeof(BudgetOverviewView), default(string));

        public static readonly BindableProperty ExpensesLabelTextProperty =
            BindableProperty.Create(nameof(ExpensesLabelText), typeof(string), typeof(BudgetOverviewView), default(string));

        public static readonly BindableProperty ExpensesValueProperty =
            BindableProperty.Create(nameof(ExpensesValue), typeof(string), typeof(BudgetOverviewView), default(string));

        public static readonly BindableProperty RemainingLabelTextProperty =
            BindableProperty.Create(nameof(RemainingLabelText), typeof(string), typeof(BudgetOverviewView), default(string));

        public static readonly BindableProperty RemainingValueProperty =
            BindableProperty.Create(nameof(RemainingValue), typeof(string), typeof(BudgetOverviewView), default(string));

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

        public string TotalLabelText
        {
            get => (string)GetValue(TotalLabelTextProperty);
            set => SetValue(TotalLabelTextProperty, value);
        }

        public string TotalValue
        {
            get => (string)GetValue(TotalValueProperty);
            set => SetValue(TotalValueProperty, value);
        }

        public string ExpensesLabelText
        {
            get => (string)GetValue(ExpensesLabelTextProperty);
            set => SetValue(ExpensesLabelTextProperty, value);
        }

        public string ExpensesValue
        {
            get => (string)GetValue(ExpensesValueProperty);
            set => SetValue(ExpensesValueProperty, value);
        }

        public string RemainingLabelText
        {
            get => (string)GetValue(RemainingLabelTextProperty);
            set => SetValue(RemainingLabelTextProperty, value);
        }

        public string RemainingValue
        {
            get => (string)GetValue(RemainingValueProperty);
            set => SetValue(RemainingValueProperty, value);
        }

         public bool IsExpensesVisible
        {
            get => (bool)GetValue(IsExpensesVisibleProperty);
            set => SetValue(IsExpensesVisibleProperty, value);
        }

        public BudgetOverviewView()
        {
            InitializeComponent();
        }
    }
}
