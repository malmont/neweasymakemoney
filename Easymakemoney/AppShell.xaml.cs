
namespace Easymakemoney;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();
		this.BindingContext = new AppShellViewModel();
        Routing.RegisterRoute("DashboardPage", typeof(DashboardPage));
		Routing.RegisterRoute("ListNewProductVariantPage", typeof(ListNewProductVariantPage));
		Routing.RegisterRoute("ListNewFraisDePortPage", typeof(ListNewFraisDePortPage));
		Routing.RegisterRoute("ListNewNoteDeFraisPage", typeof(ListNewNoteDeFraisPage));
		Routing.RegisterRoute("ListNewFournisseurPage", typeof(ListNewFournisseurPage));
		Routing.RegisterRoute("ListNewCommandPage", typeof(ListNewCommandPage));
		Routing.RegisterRoute("ListNewProductPage", typeof(ListNewProductPage));
		Routing.RegisterRoute("LoginPage", typeof(LoginPage));
        Routing.RegisterRoute("AllListsPage", typeof(AllListsPage));
        Routing.RegisterRoute("ListNewCollectionPage", typeof(ListNewCollectionPage));
    }
}


