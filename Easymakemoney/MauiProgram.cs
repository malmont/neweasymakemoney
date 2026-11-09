
using Easymakemoney.Services.Tenant;
using Easymakemoney.Services.Site;
using Easymakemoney.Services.Auth;
using Easymakemoney.Services.Session;
using Easymakemoney.Services.Dialog;

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

		// Client nommé "auth" pour les routes publiques (M1, M2) : BaseAddress + TenantHandler seulement
		builder.Services.AddHttpClient("auth", ConfigureBackendHttpClient)
			.AddHttpMessageHandler<TenantHandler>();

		// Client HTTP principal : BaseAddress + TenantHandler + AuthHandler
		builder.Services.AddHttpClient<IHttpService, HttpService>(ConfigureBackendHttpClient)
			.AddHttpMessageHandler<TenantHandler>()
			.AddHttpMessageHandler<AuthHandler>();

		builder.Services.AddHttpClient<ISiteService, SiteService>(ConfigureBackendHttpClient)
			.AddHttpMessageHandler<TenantHandler>();

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
		builder.Services.AddTransient<IRepriseService, RepriseService>();
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
		builder.Services.AddTransient<OrderNumberPage>();
		builder.Services.AddTransient<PanierMoyenPage>();
		builder.Services.AddTransient<StockPage>();
		builder.Services.AddTransient<ChiffreAffaireUseCase>();
		builder.Services.AddTransient<OrderCountUseCase>();
		builder.Services.AddTransient<AverageOrderValueUseCase>();
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
		builder.Services.AddTransient<ListNewProductVariantPage>();
		builder.Services.AddTransient<ListNewNoteDeFraisPage>();
		builder.Services.AddTransient<ListNewFournisseurPage>();
		builder.Services.AddTransient<ListNewFraisDePortPage>();
		builder.Services.AddTransient<DashBoardCollectionPage>();
		builder.Services.AddTransient<DashBoardCommandPage>();
		builder.Services.AddTransient<DashBoardProductPage>();
		builder.Services.AddTransient<StatistiquesPage>();
		builder.Services.AddTransient<ChiffresAffairePage>();
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
		builder.Services.AddTransient<StatistiquesDataValueViewmodel>();
		builder.Services.AddTransient<OrderCountViewModel>();
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

