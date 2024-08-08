
namespace Easymakemoney;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();
		this.BindingContext = new AppShellViewModel();
        Routing.RegisterRoute("DashboardPage", typeof(DashboardPage));
		Routing.RegisterRoute("ListNewCommandPage", typeof(ListNewCommandPage));
		Routing.RegisterRoute("LoginPage", typeof(LoginPage));
        Routing.RegisterRoute("AllListsPage", typeof(AllListsPage));
        Routing.RegisterRoute("ListNewCollectionPage", typeof(ListNewCollectionPage));
    }
}


