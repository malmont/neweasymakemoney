
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
		builder.Services.AddSingleton<IListCommandService, ListCommandService>();
		builder.Services.AddSingleton<IListProductService, ListProductService>();
		builder.Services.AddSingleton<IListCategoryServices, ListCategoryServices>();
		builder.Services.AddSingleton<IListStyleService, ListStyleService>();
		builder.Services.AddSingleton<IListColorsServices, ListColorsServices>();
		builder.Services.AddSingleton<IListSizesServices, ListSizesServices>();
		builder.Services.AddSingleton<IListProductVarianstService, ListProductVariantService>();
		builder.Services.AddSingleton<IListNoteDeFraisServices, ListNoteDeFraisServices>();
		builder.Services.AddSingleton<IListFourniseursServices, ListFourniseursServices>();
		builder.Services.AddSingleton<IListTransporteurService, ListTransporteurService>();
		builder.Services.AddSingleton<IListFraisDePortService, ListFraisDePortService>();
		

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



		builder.Services.AddSingleton<LoginPage>();
		builder.Services.AddSingleton<DashboardPage>();
		builder.Services.AddSingleton<LoadingPage>();
		builder.Services.AddSingleton<AllListsPage>();
		builder.Services.AddSingleton<ListNewCollectionPage>();
		builder.Services.AddSingleton<ListNewCommandPage>();
		builder.Services.AddSingleton<ListNewProductPage>();
		builder.Services.AddSingleton<ListNewProductVariantPage>();
		builder.Services.AddSingleton<ListNewNoteDeFraisPage>();
		builder.Services.AddSingleton<ListNewFournisseurPage>();
		builder.Services.AddSingleton<ListNewFraisDePortPage>();
		


		//View Models
		builder.Services.AddSingleton<LoginPageViewModel>();
		builder.Services.AddSingleton<DashboardPageViewModel>();
		builder.Services.AddSingleton<LoadingPageViewModel>();
		builder.Services.AddSingleton<AllListsViewModel>();
		builder.Services.AddSingleton<ListNewCollectionViewModel>();
		builder.Services.AddSingleton<BottomSheetPopupViewModel>();
		builder.Services.AddSingleton<ListNewCommandViewModel>();
		builder.Services.AddTransient<CollectionFormModel>();
		builder.Services.AddTransient<CommandFormModel>();
		builder.Services.AddSingleton<ListNewProductViewModel>();
		builder.Services.AddTransient<ProductFormModel>();
		builder.Services.AddTransient<ProductVariantFormModel>();
		builder.Services.AddSingleton<ListNewProductsVariantViewModel>();
		builder.Services.AddSingleton<BottomSheetPopupListViewViewModel>();
		builder.Services.AddSingleton<ListNewNoteDeFraisViewModel>();
		builder.Services.AddSingleton<NoteDeFraisForModel>();
		builder.Services.AddSingleton<FournisseurFormModel>();
		builder.Services.AddSingleton<ListNewFournisseurViewModel>();
		builder.Services.AddSingleton<FraisDePortFormModel>();
		builder.Services.AddSingleton<ListNewFraisDePortViewModel>();



#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}

