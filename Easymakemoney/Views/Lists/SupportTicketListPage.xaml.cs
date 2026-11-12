namespace Easymakemoney.Views.Lists
{
    public partial class SupportTicketListPage : ContentPage
    {
        private readonly SupportTicketListViewModel _viewModel;

        public SupportTicketListPage(SupportTicketListViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = _viewModel = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            // Page d'accueil d'un compte Assistance seul : le bouton ouvre le menu (changer de site, se déconnecter) au lieu de « Retour ».
            Header.IsRoot = Navigation.NavigationStack.Count <= 1
                && Shell.Current?.CurrentState?.Location?.OriginalString?.Contains(Easymakemoney.Utilities.HomeRoute.SupportRouteName) == true;
            await _viewModel.InitializeAsync();
        }
    }
}
