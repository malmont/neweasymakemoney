using System.Globalization;
using Easymakemoney.Models;
using Easymakemoney.Tests.Fakes;
using Easymakemoney.UseCase.LisCommandeUseCase;
using Easymakemoney.UseCase.ListCollectionUsecase;
using Easymakemoney.UseCase.ListFournisseurUseCase;
using Easymakemoney.Utilities;
using Easymakemoney.ViewModels.FormModels;
using Easymakemoney.ViewModels.Lists;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Easymakemoney.Tests.ViewModels;

[Collection("SiteCurrency")]
public sealed class StockLowAndAllListsViewModelTests : IDisposable
{
    private readonly CultureInfo _originalCulture = CultureInfo.CurrentCulture;

    public StockLowAndAllListsViewModelTests()
    {
        CultureInfo.CurrentCulture = new CultureInfo("fr-FR");
        SiteCurrency.Reset();
    }

    public void Dispose()
    {
        CultureInfo.CurrentCulture = _originalCulture;
        SiteCurrency.Reset();
    }

    private sealed class EmptyServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }

    private static (AllListsViewModel ViewModel, List<string> Routes) CreateAllLists()
    {
        var commandService = new FakeListCommandService();
        var dialog = new FakeDialogService();
        var commandVm = new ListNewCommandViewModel(
            new GetListCommandUseCase(commandService),
            null!,
            new DeleteCommandUseCase(commandService),
            new CommandFormModel(new GetListFournisseursUseCase(new FakeListFournisseursServices())),
            dialog,
            new FakeTripService());

        var routes = new List<string>();
        var vm = new AllListsViewModel(commandVm, null!, new EmptyServiceProvider(), dialog)
        {
            NavigateAsync = route =>
            {
                routes.Add(route);
                return Task.CompletedTask;
            }
        };

        return (vm, routes);
    }

    [Fact]
    public void StockLow_FromTile_PopulatesItemsIntroAndSources_AndFormatsDaysLeft()
    {
        var tile = new HomeTile
        {
            id = "low_stock",
            text = "2 articles passent bientôt sous le seuil.",
            currency = "EUR",
            sources =
            [
                new AdviceSource { label = "Articles concernés", value = 2, unit = "count" }
            ],
            figures = JArray.FromObject(new[]
            {
                new LowStockItem { productId = 1, name = "Veste", stock = 0, soldLast30Days = 8, daysLeft = 0 },
                new LowStockItem { productId = 2, name = "Chemise", stock = 4, soldLast30Days = 10, daysLeft = 3.7 }
            })
        };

        var vm = new StockLowViewModel();
        vm.ApplyQueryAttributes(new Dictionary<string, object> { ["Tile"] = tile });

        Assert.Equal("Stock bas", vm.PageTitle);
        Assert.Equal("2 articles passent bientôt sous le seuil.", vm.Intro);
        Assert.Equal("Articles concernés : 2", vm.SourcesText);
        Assert.False(vm.IsEmpty);
        Assert.Equal(2, vm.Items.Count);
        Assert.Equal("Rupture", vm.Items[0].StockText);
        Assert.Equal("À racheter maintenant", vm.Items[0].DaysLeftText);
        Assert.Equal("4 en stock", vm.Items[1].StockText);
        Assert.Equal("Environ 3 jours de vente", vm.Items[1].DaysLeftText);
    }

    [Fact]
    public void StockLow_FromAdviceItems_SetsCustomTitleAndFormatsPrices()
    {
        SiteCurrency.SetCurrency("EUR");
        var items = new List<LowStockItem>
        {
            new() { productId = 5, name = "Robe", stock = 12, soldLast30Days = 0, price = 4_900, cost = 2_000, Currency = "EUR" }
        };

        var vm = new StockLowViewModel();
        vm.ApplyQueryAttributes(new Dictionary<string, object>
        {
            ["Items"] = items,
            ["Title"] = "Articles dormants",
            ["Intro"] = "Aucune vente depuis 30 jours."
        });

        Assert.Equal("Articles dormants", vm.PageTitle);
        Assert.Equal("Aucune vente depuis 30 jours.", vm.Intro);
        Assert.Null(vm.SourcesText);
        Assert.False(vm.IsEmpty);
        var item = Assert.Single(vm.Items);
        Assert.Equal(string.Empty, item.DaysLeftText);
        Assert.True(item.HasPrice);
        Assert.Contains("Prix", item.PriceText);
        Assert.Contains("coût", item.PriceText);
    }

    [Fact]
    public void StockLow_EmptyList_SetsIsEmptyTrue_AndInvalidQueryDoesNotThrow()
    {
        var vm = new StockLowViewModel();
        vm.ApplyQueryAttributes(new Dictionary<string, object>
        {
            ["Tile"] = new HomeTile { id = "low_stock", text = "Tout va bien." }
        });

        Assert.True(vm.IsEmpty);
        Assert.Empty(vm.Items);

        // Requête inconnue : ne lève pas d'erreur et garde l'état en place
        vm.ApplyQueryAttributes(new Dictionary<string, object> { ["Inconnu"] = "valeur" });
        Assert.True(vm.IsEmpty);
        Assert.Empty(vm.Items);
    }

    [Fact]
    public async Task AllLists_PrimaryNavigationCommands_TargetExpectedManagementRoutes()
    {
        var (vm, routes) = CreateAllLists();

        await vm.NavigateToSupplierInvoiceCommand.ExecuteAsync(null);
        await vm.NavigateToToClassifyCommand.ExecuteAsync(null);
        await vm.NavigateToCollectionCommand.ExecuteAsync(null);
        await vm.NavigateToFiscalYearCommand.ExecuteAsync(null);
        await vm.NavigateToReadinessCommand.ExecuteAsync(null);
        await vm.NavigateToRepriseCommand.ExecuteAsync(null);

        Assert.Equal(
            [
                "DocumentReadPage?Type=facture",
                "AttachProductsPage",
                "ListNewCollectionPage",
                "FiscalYearPage",
                "ManagementReadinessPage",
                "ReprisePage"
            ],
            routes);
    }

    [Fact]
    public async Task AllLists_SecondaryNavigationCommands_TargetExpectedManagementRoutes()
    {
        var (vm, routes) = CreateAllLists();

        await vm.NavigateToNoteDeFraisCommand.ExecuteAsync(null);
        await vm.NavigateToNoteFournisseurCommand.ExecuteAsync(null);
        await vm.NavigateToFraisDePortCommand.ExecuteAsync(null);
        await vm.NavigateToAdvertisingCommand.ExecuteAsync(null);
        await vm.NavigateToTripsCommand.ExecuteAsync(null);
        await vm.NavigateToToReceiveCommand.ExecuteAsync(null);
        await vm.NavigateToPreparationCommand.ExecuteAsync(null);
        await vm.NavigateToPromotionsCommand.ExecuteAsync(null);
        await vm.NavigateToVariantPromotionsCommand.ExecuteAsync(null);
        await vm.NavigateToPriceBatchesCommand.ExecuteAsync(null);
        await vm.NavigateToFixedCostsCommand.ExecuteAsync(null);
        await vm.NavigateToTreasuryCommand.ExecuteAsync(null);
        await vm.NavigateToSupportCommand.ExecuteAsync(null);

        Assert.Equal(
            [
                "ListNewNoteDeFraisPage",
                "ListNewFournisseurPage",
                "ListNewFraisDePortPage",
                "CampaignListPage",
                "TripListPage",
                "ToReceivePage",
                "PreparationPage",
                "PromotionListPage",
                "VariantPromotionListPage",
                "PriceBatchListPage",
                "FixedCostsPage",
                "TreasuryPage",
                "SupportTicketListPage"
            ],
            routes);
    }
}

