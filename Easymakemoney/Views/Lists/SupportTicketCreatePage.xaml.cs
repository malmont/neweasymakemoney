namespace Easymakemoney.Views.Lists
{
    public partial class SupportTicketCreatePage : ContentPage
    {
        public SupportTicketCreatePage(SupportTicketCreateViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}
