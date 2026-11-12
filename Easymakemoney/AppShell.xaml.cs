
namespace Easymakemoney;

public partial class AppShell : Shell
{
	private readonly Easymakemoney.Services.Theme.ISiteBrandingService _branding;

	public AppShell(AppShellViewModel viewModel, Easymakemoney.Services.Theme.ISiteBrandingService branding)
	{
		_branding = branding;
		InitializeComponent();
		this.BindingContext = viewModel;
        Routing.RegisterRoute("DashboardPage", typeof(DashboardPage));
		Routing.RegisterRoute("ListNewProductVariantPage", typeof(ListNewProductVariantPage));
		Routing.RegisterRoute("ListNewFraisDePortPage", typeof(ListNewFraisDePortPage));
		Routing.RegisterRoute("ListNewNoteDeFraisPage", typeof(ListNewNoteDeFraisPage));
		Routing.RegisterRoute("DashBoardCollectionPage", typeof(DashBoardCollectionPage));
		Routing.RegisterRoute("ListNewFournisseurPage", typeof(ListNewFournisseurPage));
		Routing.RegisterRoute("ListNewCommandPage", typeof(ListNewCommandPage));
		Routing.RegisterRoute("ListNewProductPage", typeof(ListNewProductPage));
		Routing.RegisterRoute("AttachProductsPage", typeof(AttachProductsPage));
		Routing.RegisterRoute("ReprisePage", typeof(ReprisePage));
		Routing.RegisterRoute("DocumentReadPage", typeof(DocumentReadPage));
		Routing.RegisterRoute("InvoiceReviewPage", typeof(InvoiceReviewPage));
		Routing.RegisterRoute("CampaignListPage", typeof(CampaignListPage));
		Routing.RegisterRoute("CampaignFormPage", typeof(CampaignFormPage));
		Routing.RegisterRoute("CampaignProfitabilityPage", typeof(CampaignProfitabilityPage));
		Routing.RegisterRoute("StockLowPage", typeof(StockLowPage));
		Routing.RegisterRoute("PeriodBudgetPage", typeof(PeriodBudgetPage));
		Routing.RegisterRoute("TreasuryPage", typeof(TreasuryPage));
		Routing.RegisterRoute("TreasuryMovementPage", typeof(TreasuryMovementPage));
		Routing.RegisterRoute("PeriodSimulationPage", typeof(PeriodSimulationPage));
		Routing.RegisterRoute("TripListPage", typeof(TripListPage));
		Routing.RegisterRoute("TripFormPage", typeof(TripFormPage));
		Routing.RegisterRoute("TripFollowUpPage", typeof(TripFollowUpPage));
		Routing.RegisterRoute("AdvicePage", typeof(AdvicePage));
		Routing.RegisterRoute("PriceChangePage", typeof(PriceChangePage));
		Routing.RegisterRoute("PriceBatchListPage", typeof(PriceBatchListPage));
		Routing.RegisterRoute("ToReceivePage", typeof(ToReceivePage));
		Routing.RegisterRoute("PromotionCreatePage", typeof(PromotionCreatePage));
		Routing.RegisterRoute("PromotionListPage", typeof(PromotionListPage));
		Routing.RegisterRoute("PreparationPage", typeof(PreparationPage));
		Routing.RegisterRoute("FixedCostsPage", typeof(FixedCostsPage));
		Routing.RegisterRoute("YearStatisticsPage", typeof(YearStatisticsPage));
		Routing.RegisterRoute("RentalStatisticsPage", typeof(RentalStatisticsPage));
		Routing.RegisterRoute("RentalPromotionPage", typeof(RentalPromotionPage));
		Routing.RegisterRoute("RentalGridPage", typeof(RentalGridPage));
		Routing.RegisterRoute("VariantPromotionPage", typeof(VariantPromotionPage));
		Routing.RegisterRoute("VariantPromotionListPage", typeof(VariantPromotionListPage));
		Routing.RegisterRoute(Easymakemoney.Services.Module.ModuleInactiveNotifier.Route, typeof(ModuleInactivePage));
		Routing.RegisterRoute("FiscalYearPage", typeof(FiscalYearPage));
		Routing.RegisterRoute("ManagementReadinessPage", typeof(ManagementReadinessPage));
		Routing.RegisterRoute("LoginPage", typeof(LoginPage));
        Routing.RegisterRoute("AllListsPage", typeof(AllListsPage));
        Routing.RegisterRoute("ListNewCollectionPage", typeof(ListNewCollectionPage));
		Routing.RegisterRoute("DashBoardCommandPage", typeof(DashBoardCommandPage));
		Routing.RegisterRoute("DashBoardProductPage", typeof(DashBoardProductPage));
		Routing.RegisterRoute("StatistiquesPage", typeof(StatistiquesPage));
		Routing.RegisterRoute("StockPage", typeof(StockPage));
		Routing.RegisterRoute("FraisEvolutionEntreprisePage", typeof(FraisEvolutionEntreprisePage));
		Routing.RegisterRoute("PaiementRemboursementPage", typeof(PaiementRemboursementPage));
		Routing.RegisterRoute("TransactionCaissePage", typeof(TransactionCaissePage));
		Routing.RegisterRoute("FraisClientTotalViewPage", typeof(FraisClientTotalViewPage));
		
    }

	protected override void OnNavigated(ShellNavigatedEventArgs args)
	{
		base.OnNavigated(args);
		_ = _branding.EnsureLoadedAsync();
	}
}


