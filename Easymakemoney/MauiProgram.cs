
using CommunityToolkit.Maui;

using Microsoft.Extensions.Logging;

namespace Easymakemoney;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseMauiCommunityToolkit()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});



		builder.Services.AddHttpClient<IHttpService, HttpService>();

		builder.Services.AddSingleton<ILoginService, LoginService>();
		builder.Services.AddSingleton<IListCollectionService, ListCollectionService>();
		builder.Services.AddSingleton<IJwtService, JwtService>();
		builder.Services.AddSingleton<IPreferenceService, PreferenceService>();
		builder.Services.AddTransient<IListCommandService, ListCommandService>();
		builder.Services.AddTransient<IListProductService, ListProductService>();
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



		builder.Services.AddTransient<LoginPage>();
		builder.Services.AddTransient<DashboardPage>();
		builder.Services.AddTransient<LoadingPage>();
		builder.Services.AddTransient<AllListsPage>();
		builder.Services.AddTransient<ListNewCollectionPage>();
		builder.Services.AddTransient<ListNewCommandPage>();
		builder.Services.AddTransient<ListNewProductPage>();
		builder.Services.AddTransient<ListNewProductVariantPage>();
		builder.Services.AddTransient<ListNewNoteDeFraisPage>();
		builder.Services.AddTransient<ListNewFournisseurPage>();
		builder.Services.AddTransient<ListNewFraisDePortPage>();
		builder.Services.AddTransient<DashBoardCollectionPage>();
		builder.Services.AddTransient<DashBoardCommandPage>();


		//View Models
		builder.Services.AddSingleton<LoginPageViewModel>();
		builder.Services.AddSingleton<DashboardPageViewModel>();
		builder.Services.AddSingleton<LoadingPageViewModel>();
		builder.Services.AddTransient<AllListsViewModel>();
		builder.Services.AddTransient<ListNewCollectionViewModel>();
		builder.Services.AddTransient<BottomSheetPopupViewModel>();
		builder.Services.AddTransient<ListNewCommandViewModel>();
		builder.Services.AddTransient<CollectionFormModel>();
		builder.Services.AddTransient<CommandFormModel>();
		builder.Services.AddTransient<ListNewProductViewModel>();
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




#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}

