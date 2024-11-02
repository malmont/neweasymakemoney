
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
		Routing.RegisterRoute("DashBoardCollectionPage", typeof(DashBoardCollectionPage));
		Routing.RegisterRoute("ListNewFournisseurPage", typeof(ListNewFournisseurPage));
		Routing.RegisterRoute("ListNewCommandPage", typeof(ListNewCommandPage));
		Routing.RegisterRoute("ListNewProductPage", typeof(ListNewProductPage));
		Routing.RegisterRoute("LoginPage", typeof(LoginPage));
        Routing.RegisterRoute("AllListsPage", typeof(AllListsPage));
        Routing.RegisterRoute("ListNewCollectionPage", typeof(ListNewCollectionPage));
		Routing.RegisterRoute("DashBoardCommandPage", typeof(DashBoardCommandPage));
		Routing.RegisterRoute("DashBoardProductPage", typeof(DashBoardProductPage));
		Routing.RegisterRoute("StatistiquesPage", typeof(StatistiquesPage));
		Routing.RegisterRoute("ChiffresAffairePage", typeof(ChiffresAffairePage));
		Routing.RegisterRoute("OrderNumberPage", typeof(OrderNumberPage));
		Routing.RegisterRoute("PanierMoyenPage", typeof(PanierMoyenPage));
		Routing.RegisterRoute("StockPage", typeof(StockPage));
		Routing.RegisterRoute("FraisEvolutionEntreprisePage", typeof(FraisEvolutionEntreprisePage));
		Routing.RegisterRoute("PaiementRemboursementPage", typeof(PaiementRemboursementPage));
		Routing.RegisterRoute("TransactionCaissePage", typeof(TransactionCaissePage));
		Routing.RegisterRoute("FraisClientTotalViewPage", typeof(FraisClientTotalViewPage));
		
    }
}


