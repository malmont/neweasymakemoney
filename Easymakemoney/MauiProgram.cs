
using Easymakemoney.Services.Tenant;
using Easymakemoney.Services.Site;
using Easymakemoney.Services.Auth;
using Easymakemoney.Services.Session;
using Easymakemoney.Services.Dialog;
using Easymakemoney.Services.Device;

namespace Easymakemoney;

public static class MauiProgram
{
	private static void ConfigureBackendHttpClient(HttpClient client)
	{
		client.BaseAddress = new Uri(Configurations.BackendSymfonyUrl);
	}

	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseMauiCommunityToolkit(options =>
			{
				options.SetPopupDefaults(new DefaultPopupSettings
				{
					BackgroundColor = Colors.Transparent,
					Margin = 0,
					Padding = 0
				});
				options.SetPopupOptionsDefaults(new DefaultPopupOptionsSettings
				{
					Shape = null,
					Shadow = null
				});
			})
			.UseSkiaSharp()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
				fonts.AddFont("Inter-Regular.ttf", "Inter");
				fonts.AddFont("Inter-Medium.ttf", "InterMedium");
				fonts.AddFont("Inter-SemiBold.ttf", "InterSemiBold");
				fonts.AddFont("Inter-Bold.ttf", "InterBold");
			});



		// Configuration HTTP partagée (L5, M1) et gestionnaires de requêtes
		builder.Services.AddTransient<TenantHandler>();
		builder.Services.AddTransient<AuthHandler>();
		builder.Services.AddTransient<Easymakemoney.Services.Module.ModuleGateHandler>();
		builder.Services.AddSingleton<Easymakemoney.Services.Offline.IResponseCache, Easymakemoney.Services.Offline.FileResponseCache>();
		builder.Services.AddTransient<Easymakemoney.Services.Offline.OfflineCacheHandler>();
		builder.Services.AddSingleton<Easymakemoney.Services.Module.IModuleInactiveNotifier, Easymakemoney.Services.Module.ModuleInactiveNotifier>();

		// Client nommé "auth" pour les routes publiques (M1, M2) : BaseAddress + TenantHandler seulement
		builder.Services.AddHttpClient("auth", ConfigureBackendHttpClient)
			.AddHttpMessageHandler<TenantHandler>();

		// Client HTTP principal : BaseAddress + TenantHandler + AuthHandler
		builder.Services.AddHttpClient<IHttpService, HttpService>(ConfigureBackendHttpClient)
			.AddHttpMessageHandler<TenantHandler>()
			.AddHttpMessageHandler<AuthHandler>()
			.AddHttpMessageHandler<Easymakemoney.Services.Module.ModuleGateHandler>()
			// En dernier : la requête porte déjà le site et le jeton ; sans réseau, sert la dernière copie (statistiques, trésorerie)
			.AddHttpMessageHandler<Easymakemoney.Services.Offline.OfflineCacheHandler>();

		builder.Services.AddHttpClient<ISiteService, SiteService>(ConfigureBackendHttpClient)
			.AddHttpMessageHandler<TenantHandler>();

		builder.Services.AddSingleton<IDeviceInfoService, MauiDeviceInfoService>();
		builder.Services.AddSingleton<ITenantStore, TenantStore>();
		builder.Services.AddSingleton<Easymakemoney.Services.Theme.ISiteBrandingService, Easymakemoney.Services.Theme.SiteBrandingService>();
		builder.Services.AddSingleton<ISiteDataCleaner, SiteDataCleaner>();
		builder.Services.AddSingleton<ISessionService, SessionService>();
		builder.Services.AddSingleton<IPendingLoginStore, PendingLoginStore>();
		builder.Services.AddSingleton<ISessionExpiryNotifier, SessionExpiryNotifier>();
		builder.Services.AddSingleton<ITokenRefresher, TokenRefresher>();
		builder.Services.AddSingleton<IAuthFlowService, AuthFlowService>();
		builder.Services.AddSingleton<IDialogService, DialogService>();
		builder.Services.AddSingleton<AppShellViewModel>();
		builder.Services.AddSingleton<AppShell>();

		builder.Services.AddTransient<ILoginService, LoginService>();
		builder.Services.AddTransient<IListCollectionService, ListCollectionService>();
		builder.Services.AddSingleton<IJwtService, JwtService>();
		builder.Services.AddTransient<IListCommandService, ListCommandService>();
		builder.Services.AddTransient<IListProductService, ListProductService>();
		builder.Services.AddTransient<IProductAttachmentService, ProductAttachmentService>();
		builder.Services.AddTransient<Easymakemoney.UseCase.ProductAttachmentUseCase.GetAvailableProductsUseCase>();
		builder.Services.AddTransient<Easymakemoney.UseCase.ProductAttachmentUseCase.AttachProductsUseCase>();
		builder.Services.AddTransient<Easymakemoney.UseCase.ProductAttachmentUseCase.DetachProductUseCase>();
		builder.Services.AddTransient<IListCategoryServices, ListCategoryServices>();
		builder.Services.AddTransient<IListStyleService, ListStyleService>();
		builder.Services.AddTransient<IListColorsServices, ListColorsServices>();
		builder.Services.AddTransient<IListSizesServices, ListSizesServices>();
		builder.Services.AddTransient<IListProductVarianstService, ListProductVariantService>();
		builder.Services.AddTransient<IListNoteDeFraisServices, ListNoteDeFraisServices>();
		builder.Services.AddTransient<IListFourniseursServices, ListFourniseursServices>();
		builder.Services.AddTransient<IListTransporteurService, ListTransporteurService>();
		builder.Services.AddTransient<IListFraisDePortService, ListFraisDePortService>();
		builder.Services.AddTransient<IDashBoardCollectionService, DashBoardCollectionService>();
		builder.Services.AddTransient<IDashboardCommandServices, DashboardCommandServices>();
		builder.Services.AddTransient<IChiffreAffaireService, ChiffreAffaireService>();
		builder.Services.AddTransient<IOrderCountServices, OrderCountServices>();
		builder.Services.AddTransient<IAverageOrderValueServices, AverageOrderValueServices>();
		builder.Services.AddTransient<IStockEvolutionServices, StockEvolutionServices>();
		builder.Services.AddTransient<IFraisEvolutionService, FraisEvolutionService>();
		builder.Services.AddTransient<IPaiementRemboursementService, PaiementRemboursementService>();
		builder.Services.AddTransient<ITransactionCaisseService, TransactionCaisseService>();
		builder.Services.AddTransient<IFraisClientTotalService, FraisClientTotalService>();
		builder.Services.AddTransient<ICollectionCloseService, CollectionCloseService>();
		builder.Services.AddTransient<IPeriodResultService, PeriodResultService>();
		builder.Services.AddTransient<IUnifiedStatisticsService, UnifiedStatisticsService>();
		builder.Services.AddTransient<IYearStatisticsService, YearStatisticsService>();
		builder.Services.AddTransient<IRentalStatisticsService, RentalStatisticsService>();
		builder.Services.AddTransient<IRentalPromotionService, RentalPromotionService>();
		builder.Services.AddTransient<IRentalGridService, RentalGridService>();
		builder.Services.AddTransient<IVariantPromotionService, VariantPromotionService>();
		builder.Services.AddTransient<IManagementModuleService, ManagementModuleService>();
		builder.Services.AddTransient<ISupportService, SupportService>();
		builder.Services.AddTransient<IManagementEditService, ManagementEditService>();
		builder.Services.AddSingleton<Easymakemoney.Services.Offline.IHomeSnapshotStore, Easymakemoney.Services.Offline.HomeSnapshotStore>();
		builder.Services.AddTransient<IPeriodService, PeriodService>();
		builder.Services.AddTransient<Easymakemoney.UseCase.PeriodResultUseCase.PeriodUseCase>();
		builder.Services.AddTransient<IRepriseService, RepriseService>();
		builder.Services.AddTransient<IDocumentReaderService, DocumentReaderService>();
		builder.Services.AddTransient<IInvoiceConversationService, InvoiceConversationService>();
		builder.Services.AddTransient<ICommandeReceptionService, CommandeReceptionService>();
		builder.Services.AddTransient<ICampaignService, CampaignService>();
		builder.Services.AddTransient<IHomeTilesService, HomeTilesService>();
		builder.Services.AddTransient<IPeriodBudgetService, PeriodBudgetService>();
		builder.Services.AddTransient<ITreasuryService, TreasuryService>();
		builder.Services.AddTransient<ISimulationService, SimulationService>();
		builder.Services.AddTransient<ITripService, TripService>();
		builder.Services.AddTransient<IAdviceService, AdviceService>();
		builder.Services.AddTransient<IPriceService, PriceService>();
		builder.Services.AddTransient<IToReceiveService, ToReceiveService>();
		builder.Services.AddTransient<IPromotionService, PromotionService>();
		builder.Services.AddTransient<IPreparationService, PreparationService>();
		builder.Services.AddTransient<Easymakemoney.UseCase.DocumentUseCase.DocumentReaderUseCase>();
		builder.Services.AddTransient<Easymakemoney.UseCase.RepriseUseCase.RepriseUseCase>();
		builder.Services.AddTransient<Easymakemoney.UseCase.PeriodResultUseCase.GetPeriodResultUseCase>();


		builder.Services.AddTransient<GetListCollectionsUseCase>();
		builder.Services.AddTransient<CreateCollectionUseCase>();
		builder.Services.AddTransient<LoginUseCase>();
		builder.Services.AddTransient<DeleteCollectionUseCase>();
		builder.Services.AddTransient<GetListCommandUseCase>();
		builder.Services.AddTransient<CreateCommandUseCase>();
		builder.Services.AddTransient<DeleteCommandUseCase>();
		builder.Services.AddTransient<GetListProductsUseCase>();
		builder.Services.AddTransient<CreateProductsUseCase>();
		builder.Services.AddTransient<GetListCategoryUseCase>();
		builder.Services.AddTransient<GetListStyleUsesCases>();
		builder.Services.AddTransient<SaveCommandUseCase>();
		builder.Services.AddTransient<SaveProductUseCase>();
		builder.Services.AddTransient<SaveCollectionUseCase>();
		builder.Services.AddTransient<DeleteProductsUseCase>();
		builder.Services.AddTransient<DeleteProductsVariantUseCase>();
		builder.Services.AddTransient<GetListColorsUseCase>();
		builder.Services.AddTransient<GetListSizesUseCase>();
		builder.Services.AddTransient<GetListProductsVariantUseCase>();
		builder.Services.AddTransient<CreateProductsVariantUseCase>();
		builder.Services.AddTransient<SaveProductvariantUseCase>();
		builder.Services.AddTransient<DeleteNoteDeFraisUseCase>();
		builder.Services.AddTransient<GetListNoteDeFraisUseCase>();
		builder.Services.AddTransient<CreateNoteDeFraisUseCase>();
		builder.Services.AddTransient<SaveNoteDeFraisUseCase>();
		builder.Services.AddTransient<DeleteFournisseurUseCase>();
		builder.Services.AddTransient<GetListFournisseursUseCase>();
		builder.Services.AddTransient<CreateFournisseurUseCase>();
		builder.Services.AddTransient<SaveFournisseurUseCase>();
		builder.Services.AddTransient<DeleteFraisDePortUseCase>();
		builder.Services.AddTransient<GetListFraisDePortUseCase>();
		builder.Services.AddTransient<CreateFraisDePortUseCase>();
		builder.Services.AddTransient<SaveFraisDePortUseCase>();
		builder.Services.AddTransient<GetListTransporteurUseCase>();
		builder.Services.AddTransient<GetDashBoardCollectionUseCase>();
		builder.Services.AddTransient<GetDashboardCommandUseCase>();
		builder.Services.AddTransient<StockPage>();
		builder.Services.AddTransient<StockEvolutionUseCase>();
		builder.Services.AddTransient<StockValueEvolutionUseCase>();
		builder.Services.AddTransient<FraisEvolutionUseCase>();
		builder.Services.AddTransient<PaiementRemboursementUseCase>();
		builder.Services.AddTransient<TransactionCaisseUseCase>();
		builder.Services.AddTransient<GetFraisClientTaxUseCase>();
		builder.Services.AddTransient<GetFraisClientCarrierUseCase>();
		builder.Services.AddTransient<GetCollectionCloseUseCase>();
		builder.Services.AddTransient<GetListTypeFournisseurUseCase>();
		builder.Services.AddTransient<GestListTypeNoteDeFraisUseCase>();



		builder.Services.AddTransient<ActivationPage>();
		builder.Services.AddTransient<LoginPage>();
		builder.Services.AddTransient<OtpPage>();
		builder.Services.AddTransient<DashboardPage>();
		builder.Services.AddTransient<LoadingPage>();
		builder.Services.AddTransient<AllListsPage>();
		builder.Services.AddTransient<ListNewCollectionPage>();
		builder.Services.AddTransient<ListNewCommandPage>();
		builder.Services.AddTransient<ListNewProductPage>();
		builder.Services.AddTransient<AttachProductsPage>();
		builder.Services.AddTransient<ReprisePage>();
		builder.Services.AddTransient<DocumentReadPage>();
		builder.Services.AddTransient<InvoiceReviewPage>();
		builder.Services.AddTransient<InvoiceConversationPage>();
		builder.Services.AddTransient<CampaignListPage>();
		builder.Services.AddTransient<CampaignFormPage>();
		builder.Services.AddTransient<CampaignProfitabilityPage>();
		builder.Services.AddTransient<StockLowPage>();
		builder.Services.AddTransient<PeriodBudgetPage>();
		builder.Services.AddTransient<TreasuryPage>();
		builder.Services.AddTransient<TreasuryMovementPage>();
		builder.Services.AddTransient<PeriodSimulationPage>();
		builder.Services.AddTransient<TripListPage>();
		builder.Services.AddTransient<TripFormPage>();
		builder.Services.AddTransient<TripFollowUpPage>();
		builder.Services.AddTransient<AdvicePage>();
		builder.Services.AddTransient<ModuleInactivePage>();
		builder.Services.AddTransient<ManagementReadinessPage>();
		builder.Services.AddTransient<SupportTicketListPage>();
		builder.Services.AddTransient<SupportTicketPage>();
		builder.Services.AddTransient<SupportTicketCreatePage>();
		builder.Services.AddTransient<SupplierEditPage>();
		builder.Services.AddTransient<ShippingEditPage>();
		builder.Services.AddTransient<PriceChangePage>();
		builder.Services.AddTransient<PriceBatchListPage>();
		builder.Services.AddTransient<ToReceivePage>();
		builder.Services.AddTransient<PromotionCreatePage>();
		builder.Services.AddTransient<PromotionListPage>();
		builder.Services.AddTransient<PreparationPage>();
		builder.Services.AddTransient<FixedCostsPage>();
		builder.Services.AddTransient<YearStatisticsPage>();
		builder.Services.AddTransient<RentalStatisticsPage>();
		builder.Services.AddTransient<RentalPromotionPage>();
		builder.Services.AddTransient<RentalGridPage>();
		builder.Services.AddTransient<VariantPromotionPage>();
		builder.Services.AddTransient<VariantPromotionListPage>();
		builder.Services.AddTransient<FiscalYearPage>();
		builder.Services.AddTransient<ListNewProductVariantPage>();
		builder.Services.AddTransient<ListNewNoteDeFraisPage>();
		builder.Services.AddTransient<ListNewFournisseurPage>();
		builder.Services.AddTransient<ListNewFraisDePortPage>();
		builder.Services.AddTransient<DashBoardCollectionPage>();
		builder.Services.AddTransient<DashBoardCommandPage>();
		builder.Services.AddTransient<DashBoardProductPage>();
		builder.Services.AddTransient<StatistiquesPage>();
		builder.Services.AddTransient<FraisEvolutionEntreprisePage>();
		builder.Services.AddTransient<PaiementRemboursementPage>();
		builder.Services.AddTransient<TransactionCaissePage>();
		builder.Services.AddTransient<FraisClientTotalViewPage>();


		//View Models
		builder.Services.AddTransient<ActivationPageViewModel>();
		builder.Services.AddTransient<LoginPageViewModel>();
		builder.Services.AddTransient<OtpPageViewModel>();
		builder.Services.AddTransient<DashboardPageViewModel>();
		builder.Services.AddTransient<LoadingPageViewModel>();
		builder.Services.AddTransient<AllListsViewModel>();
		builder.Services.AddTransient<ListNewCollectionViewModel>();
		builder.Services.AddTransient<BottomSheetPopupViewModel>();
		builder.Services.AddTransient<ListNewCommandViewModel>();
		builder.Services.AddTransient<CollectionFormModel>();
		builder.Services.AddTransient<CommandFormModel>();
		builder.Services.AddTransient<ListNewProductViewModel>();
		builder.Services.AddTransient<AttachProductsViewModel>();
		builder.Services.AddTransient<RepriseViewModel>();
		builder.Services.AddTransient<DocumentReadViewModel>();
		builder.Services.AddTransient<InvoiceReviewViewModel>();
		builder.Services.AddTransient<InvoiceConversationViewModel>();
		builder.Services.AddTransient<CampaignListViewModel>();
		builder.Services.AddTransient<CampaignFormViewModel>();
		builder.Services.AddTransient<CampaignProfitabilityViewModel>();
		builder.Services.AddTransient<StockLowViewModel>();
		builder.Services.AddTransient<PeriodBudgetViewModel>();
		builder.Services.AddTransient<TreasuryViewModel>();
		builder.Services.AddTransient<TreasuryMovementViewModel>();
		builder.Services.AddTransient<PeriodSimulationViewModel>();
		builder.Services.AddTransient<TripListViewModel>();
		builder.Services.AddTransient<TripFormViewModel>();
		builder.Services.AddTransient<TripFollowUpViewModel>();
		builder.Services.AddTransient<AdviceViewModel>();
		builder.Services.AddTransient<ModuleInactiveViewModel>();
		builder.Services.AddTransient<ManagementReadinessViewModel>();
		builder.Services.AddTransient<SupportTicketListViewModel>();
		builder.Services.AddTransient<SupportTicketViewModel>();
		builder.Services.AddTransient<SupportTicketCreateViewModel>();
		builder.Services.AddTransient<SupplierEditViewModel>();
		builder.Services.AddTransient<ShippingEditViewModel>();
		builder.Services.AddTransient<PriceChangeViewModel>();
		builder.Services.AddTransient<PriceBatchListViewModel>();
		builder.Services.AddTransient<ToReceiveViewModel>();
		builder.Services.AddTransient<PromotionCreateViewModel>();
		builder.Services.AddTransient<PromotionListViewModel>();
		builder.Services.AddTransient<PreparationViewModel>();
		builder.Services.AddTransient<FixedCostsViewModel>();
		builder.Services.AddTransient<YearStatisticsViewModel>();
		builder.Services.AddTransient<RentalStatisticsViewModel>();
		builder.Services.AddTransient<RentalPromotionViewModel>();
		builder.Services.AddTransient<RentalGridViewModel>();
		builder.Services.AddTransient<VariantPromotionViewModel>();
		builder.Services.AddTransient<VariantPromotionListViewModel>();
		builder.Services.AddTransient<FiscalYearViewModel>();
		builder.Services.AddTransient<ProductFormModel>();
		builder.Services.AddTransient<ProductVariantFormModel>();
		builder.Services.AddTransient<ListNewProductsVariantViewModel>();
		builder.Services.AddTransient<BottomSheetPopupListViewViewModel>();
		builder.Services.AddTransient<ListNewNoteDeFraisViewModel>();
		builder.Services.AddTransient<NoteDeFraisForModel>();
		builder.Services.AddTransient<FournisseurFormModel>();
		builder.Services.AddTransient<ListNewFournisseurViewModel>();
		builder.Services.AddTransient<FraisDePortFormModel>();
		builder.Services.AddTransient<ListNewFraisDePortViewModel>();
		builder.Services.AddTransient<DashBoardCollectionViewModel>();
		builder.Services.AddTransient<DashBoardCommandViewModel>();
		builder.Services.AddTransient<DashBoardProductViewModel>();
		builder.Services.AddTransient<StatistiquesViewModel>();
		builder.Services.AddTransient<StockEvolutionViewModel>();
		builder.Services.AddTransient<FraisEvolutionEntrepriseViewModel>();
		builder.Services.AddTransient<PaiementRemboursementViewModel>();
		builder.Services.AddTransient<TransactionCaisseViewModel>();
		builder.Services.AddTransient<FraisClientTotalViewModel>();






#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}

