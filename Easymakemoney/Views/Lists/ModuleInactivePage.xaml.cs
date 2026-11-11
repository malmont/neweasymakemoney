using Easymakemoney.Services.Module;

namespace Easymakemoney.Views.Lists
{
    public partial class ModuleInactivePage : ContentPage
    {
        private readonly ModuleInactiveViewModel _viewModel;
        private readonly IModuleInactiveNotifier _notifier;

        public ModuleInactivePage(ModuleInactiveViewModel viewModel, IModuleInactiveNotifier notifier)
        {
            InitializeComponent();
            BindingContext = _viewModel = viewModel;
            _notifier = notifier;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            _viewModel.Refresh();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            // L'écran fermé (retour ou accueil) : un prochain refus le rouvrira.
            _notifier.Reset();
        }

        // Retour matériel (Android) : retour à l'accueil plutôt qu'à un écran qui serait refusé à nouveau.
        protected override bool OnBackButtonPressed()
        {
            _viewModel.RetryCommand.Execute(null);
            return true;
        }
    }
}
