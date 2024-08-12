
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

		builder.Services.AddTransient<GetListCollectionsUseCase>();
		builder.Services.AddTransient<CreateCollectionUseCase>();
		builder.Services.AddTransient<LoginUseCase>();
		builder.Services.AddTransient<DeleteCollectionUseCase>();
		builder.Services.AddTransient<GetListCommandUseCase>();
		builder.Services.AddTransient<CreateCommandUseCase>();
		builder.Services.AddTransient<DeleteCommandUseCase>();
		builder.Services.AddTransient<GetListProductsUseCase>();

		builder.Services.AddSingleton<LoginPage>();
		builder.Services.AddSingleton<DashboardPage>();
		builder.Services.AddSingleton<LoadingPage>();
		builder.Services.AddSingleton<AllListsPage>();
		builder.Services.AddSingleton<ListNewCollectionPage>();
		builder.Services.AddSingleton<ListNewCommandPage>();
		builder.Services.AddSingleton<ListNewProductPage>();
		


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
#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}

