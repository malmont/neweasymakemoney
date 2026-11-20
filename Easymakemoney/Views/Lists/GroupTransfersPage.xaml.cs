namespace Easymakemoney.Views.Lists
{
    /// <summary>Transferts de stock à envoyer à une autre boutique du groupe (lot backend G-5).</summary>
    public partial class GroupTransfersPage : ContentPage
    {
        private readonly GroupTransfersViewModel _viewModel;

        public GroupTransfersPage(GroupTransfersViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = _viewModel = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.AppearingAsync();
        }

        protected override void OnDisappearing()
        {
            // La relecture d'un envoi en vérification s'arrête quand on quitte l'écran.
            _viewModel.Disappearing();
            base.OnDisappearing();
        }
    }
}
