using System.Globalization;
using System.Net;
using Easymakemoney.Models;
using Easymakemoney.Models.Api;
using Easymakemoney.Services.Api;
using Easymakemoney.Tests.Fakes;
using Easymakemoney.UseCase.DocumentUseCase;
using Easymakemoney.UseCase.ProductAttachmentUseCase;
using Easymakemoney.Utilities;
using Easymakemoney.ViewModels.Lists;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Easymakemoney.Tests.ViewModels;

[Collection("SiteCurrency")]
public class CampaignViewModelsTests : IDisposable
{
    private readonly CultureInfo _originalCulture;
    private readonly FakeCampaignService _campaignService = new();
    private readonly FakeDocumentReaderService _documentReaderService = new();
    private readonly FakeProductAttachmentService _productAttachmentService = new();
    private readonly FakeDialogService _dialogService = new();

    public CampaignViewModelsTests()
    {
        _originalCulture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("fr-FR");
        SiteCurrency.Reset();
        SiteCurrency.SetCurrency("CAD");
    }

    public void Dispose()
    {
        SiteCurrency.Reset();
        CultureInfo.CurrentCulture = _originalCulture;
    }

    private CampaignListViewModel CreateListViewModel() => new(_campaignService, _dialogService);
    private CampaignProfitabilityViewModel CreateProfitabilityViewModel() => new(_campaignService);
    private CampaignFormViewModel CreateFormViewModel()
    {
        var documentUseCase = new DocumentReaderUseCase(_documentReaderService);
        var productUseCase = new GetAvailableProductsUseCase(_productAttachmentService);
        return new CampaignFormViewModel(_campaignService, documentUseCase, productUseCase, _dialogService);
    }

    // =========================================================================
    // CampaignListViewModel
    // =========================================================================

    [Fact]
    public async Task CampaignList_LoadFirstPageAsync_PopulatesListAndSetsEmptyState()
    {
        var vm = CreateListViewModel();
        _campaignService.ListResult = new List<Campaign>
        {
            new() { id = 1, name = "Promo Hiver", amount = 10000, channel = "facebook" },
            new() { id = 2, name = "Soldes Été", amount = 25000, channel = "instagram" }
        };

        await vm.LoadFirstPageAsync();

        Assert.Equal(2, vm.Campaigns.Count);
        Assert.False(vm.ShowEmptyState);
        Assert.Equal("Aucune campagne", vm.EmptyTitle);
        var call = Assert.Single(_campaignService.ListCalls);
        Assert.Equal((1, 50), call);
        Assert.Empty(_dialogService.ShownErrors);
    }

    [Fact]
    public async Task CampaignList_LoadFirstPageAsync_Empty_ShowsEmptyPlaceholder()
    {
        var vm = CreateListViewModel();
        _campaignService.ListResult = new List<Campaign>();

        await vm.LoadFirstPageAsync();

        Assert.Empty(vm.Campaigns);
        Assert.True(vm.ShowEmptyState);
        Assert.Equal("Aucune campagne", vm.EmptyTitle);
        Assert.Contains("Nouvelle campagne", vm.EmptyText);
        Assert.Empty(_dialogService.ShownErrors);
    }

    [Fact]
    public async Task CampaignList_LoadFirstPageAsync_Error_ShowsErrorDialogAndErrorPlaceholder()
    {
        var vm = CreateListViewModel();
        _campaignService.ListException = new HttpRequestException("Réseau indisponible");

        await vm.LoadFirstPageAsync();

        Assert.Empty(vm.Campaigns);
        Assert.True(vm.ShowEmptyState);
        Assert.Equal("Chargement impossible", vm.EmptyTitle);
        var error = Assert.Single(_dialogService.ShownErrors);
        Assert.IsType<HttpRequestException>(error);
    }

    [Fact]
    public async Task CampaignList_LoadMore_LoadsNextPageAndAppendsWithoutDuplicates()
    {
        var vm = CreateListViewModel();
        // Première page pleine de 50 éléments (id 1 à 50)
        var firstPage = Enumerable.Range(1, 50)
            .Select(i => new Campaign { id = i, name = $"Campagne {i}" })
            .ToList();
        _campaignService.ListResult = firstPage;
        await vm.LoadFirstPageAsync();

        Assert.Equal(50, vm.Campaigns.Count);

        // Seconde page avec l'élément 50 (décalage concurrent) et 51, 52
        _campaignService.ListResult = new List<Campaign>
        {
            new() { id = 50, name = "Campagne 50" }, // Doublon ignoré
            new() { id = 51, name = "Campagne 51" },
            new() { id = 52, name = "Campagne 52" }
        };

        await vm.LoadMoreCommand.ExecuteAsync(null);

        Assert.Equal(52, vm.Campaigns.Count);
        Assert.Equal((2, 50), _campaignService.ListCalls.Last());
        Assert.Empty(_dialogService.ShownErrors);
    }

    [Fact]
    public async Task CampaignList_Delete_ConfirmationRefused_DoesNotSendRequest()
    {
        var vm = CreateListViewModel();
        var campaign = new Campaign { id = 5, name = "Campagne À Garder" };
        vm.Campaigns.Add(campaign);

        _dialogService.ConfirmResult = false; // Gérant refuse
        await vm.DeleteCommand.ExecuteAsync(campaign);

        Assert.Single(_dialogService.ConfirmCalls);
        Assert.Contains("Campagne À Garder", _dialogService.ConfirmCalls[0].Message);
        Assert.Empty(_campaignService.DeleteCalls);
        Assert.Single(vm.Campaigns);
    }

    [Fact]
    public async Task CampaignList_Delete_Confirmed_RemovesFromList()
    {
        var vm = CreateListViewModel();
        var campaign = new Campaign { id = 6, name = "Campagne À Supprimer" };
        vm.Campaigns.Add(campaign);

        _dialogService.ConfirmResult = true; // Gérant confirme
        await vm.DeleteCommand.ExecuteAsync(campaign);

        Assert.Single(_campaignService.DeleteCalls);
        Assert.Equal(6, _campaignService.DeleteCalls[0]);
        Assert.Empty(vm.Campaigns);
        Assert.True(vm.ShowEmptyState);
        Assert.Empty(_dialogService.ShownErrors);
    }

    [Fact]
    public async Task CampaignList_Delete_404CampaignNotFound_ShowsInfoAndRemovesFromList()
    {
        var vm = CreateListViewModel();
        var campaign = new Campaign { id = 7, name = "Campagne Déjà Supprimée" };
        vm.Campaigns.Add(campaign);

        var apiError = ApiError.FromHttpResponse("""{ "error": "CAMPAIGN_NOT_FOUND" }""", HttpStatusCode.NotFound, null);
        _campaignService.DeleteException = new ApiException(apiError, HttpStatusCode.NotFound);
        _dialogService.ConfirmResult = true;

        await vm.DeleteCommand.ExecuteAsync(campaign);

        // Affiche l'information et retire de la collection
        Assert.Contains("Cette campagne a déjà été supprimée.", _dialogService.ShownInfos);
        Assert.Empty(vm.Campaigns);
        Assert.Empty(_dialogService.ShownErrors);
    }

    [Fact]
    public async Task CampaignList_Delete_409PeriodClosed_ShowsInfoAndKeepsInList()
    {
        var vm = CreateListViewModel();
        var campaign = new Campaign { id = 8, name = "Campagne Clôturée" };
        vm.Campaigns.Add(campaign);

        var apiError = ApiError.FromHttpResponse("""{ "error": "PERIOD_CLOSED" }""", HttpStatusCode.Conflict, null);
        _campaignService.DeleteException = new ApiException(apiError, HttpStatusCode.Conflict);
        _dialogService.ConfirmResult = true;

        await vm.DeleteCommand.ExecuteAsync(campaign);

        // Affiche l'explication et conserve l'élément dans la collection
        Assert.Contains("Cette campagne est dans une période clôturée : elle ne peut plus être supprimée. Rouvrez la période depuis le tableau de bord si besoin.", _dialogService.ShownInfos);
        Assert.Single(vm.Campaigns);
        Assert.Empty(_dialogService.ShownErrors);
    }

    [Fact]
    public async Task CampaignList_Commands_WithoutShell_ExecuteSafely()
    {
        var vm = CreateListViewModel();
        var campaign = new Campaign { id = 10, name = "Campagne Navigation" };

        var newEx = await Record.ExceptionAsync(() => vm.NewCampaignCommand.ExecuteAsync(null));
        var openEx = await Record.ExceptionAsync(() => vm.OpenCommand.ExecuteAsync(campaign));
        var editEx = await Record.ExceptionAsync(() => vm.EditCommand.ExecuteAsync(campaign));

        Assert.Null(newEx);
        Assert.Null(openEx);
        Assert.Null(editEx);
    }

    // =========================================================================
    // CampaignProfitabilityViewModel
    // =========================================================================

    [Fact]
    public async Task CampaignProfitability_InitAsync_LoadsCampaignAndReport()
    {
        var vm = CreateProfitabilityViewModel();
        _campaignService.CampaignResult = new Campaign { id = 15, name = "Campagne Hiver", amount = 10000 };

        _campaignService.ProfitabilityResult = JsonConvert.DeserializeObject<CampaignProfitability>("""
        {
            "campaign": { "id": 15, "name": "Campagne Hiver" }, "estimate": true, "method": "before_after", "currency": "CAD",
            "group": { "campaignIds": [15], "amount": 10000, "from": "2026-11-01", "to": "2026-11-10", "shared": false },
            "window": { "from": "2026-11-01", "to": "2026-11-10", "days": 10, "complete": true },
            "reference": { "from": "2026-10-22", "to": "2026-10-31", "days": 10 },
            "scope": { "kind": "all", "productIds": [] },
            "during": { "revenue": 150000, "grossMargin": 60000, "orders": 30, "items": 45, "unknownCostRevenue": 0 },
            "before": { "revenue": 100000, "grossMargin": 40000, "orders": 20, "items": 30, "unknownCostRevenue": 0 },
            "uplift": { "revenue": 50000, "grossMargin": 20000 },
            "gainPerDollar": { "revenue": 5.0, "grossMargin": 2.0 },
            "caveats": []
        }
        """);

        await vm.InitAsync(15);

        Assert.True(vm.IsLoaded);
        Assert.Null(vm.LoadError);
        Assert.False(vm.HasLoadError);
        Assert.NotNull(vm.Campaign);
        Assert.Equal("Campagne Hiver", vm.Campaign!.name);
        Assert.NotNull(vm.Report);
        Assert.Equal(CampaignGainLevel.Good, vm.Report!.Level);
        Assert.Single(_campaignService.GetCalls);
        Assert.Single(_campaignService.GetProfitabilityCalls);
    }

    [Fact]
    public async Task CampaignProfitability_InitAsync_404NotFound_SetsLoadError()
    {
        var vm = CreateProfitabilityViewModel();
        var apiError = ApiError.FromHttpResponse("""{ "error": "CAMPAIGN_NOT_FOUND" }""", HttpStatusCode.NotFound, null);
        _campaignService.GetException = new ApiException(apiError, HttpStatusCode.NotFound);

        await vm.InitAsync(16);

        Assert.False(vm.IsLoaded);
        Assert.True(vm.HasLoadError);
        Assert.Equal("Cette campagne n'existe plus.", vm.LoadError);
    }

    [Fact]
    public async Task CampaignProfitability_InitAsync_NullReport_SetsUnavailableError()
    {
        var vm = CreateProfitabilityViewModel();
        _campaignService.CampaignResult = new Campaign { id = 17, name = "Campagne Récente" };
        _campaignService.ProfitabilityResult = null; // Données indisponibles

        await vm.InitAsync(17);

        Assert.False(vm.IsLoaded);
        Assert.True(vm.HasLoadError);
        Assert.Equal("Rentabilité indisponible pour le moment.", vm.LoadError);
    }

    [Fact]
    public async Task CampaignProfitability_InitAsync_NetworkError_SetsGenericError()
    {
        var vm = CreateProfitabilityViewModel();
        _campaignService.GetException = new HttpRequestException("Connexion perdue");

        await vm.InitAsync(18);

        Assert.False(vm.IsLoaded);
        Assert.True(vm.HasLoadError);
        Assert.Equal("Impossible de calculer la rentabilité. Vérifiez la connexion et réessayez.", vm.LoadError);
    }

    // =========================================================================
    // CampaignFormViewModel
    // =========================================================================

    [Fact]
    public async Task CampaignForm_InitAsync_CreateMode_DefaultsToSiteCurrencyAndWholeStore()
    {
        var vm = CreateFormViewModel();

        await vm.InitAsync(0, 0);

        Assert.Equal(0, vm.CampaignId);
        Assert.Equal(0, vm.DocumentId);
        Assert.False(vm.IsEdit);
        Assert.False(vm.HasDocument);
        Assert.True(vm.CanReadInvoice);
        Assert.Equal("Nouvelle campagne", vm.PageTitle);
        Assert.Equal(CampaignFormViewModel.SiteCurrencyChoice, vm.SelectedCurrency);
        Assert.False(vm.NeedsExchangeRate);
        Assert.Equal("Toutes les ventes du magasin", vm.TargetSummary);
        Assert.True(vm.IsLoaded);
    }

    [Fact]
    public async Task CampaignForm_InitAsync_EditMode_LoadsCampaignAndPopulatesFields()
    {
        var vm = CreateFormViewModel();
        _campaignService.CampaignResult = new Campaign
        {
            id = 20,
            name = "Campagne Automne",
            channel = "instagram",
            startDate = "2026-11-01",
            endDate = "2026-11-10",
            amount = 15000,
            currency = "CAD",
            siteCurrency = "CAD",
            productIds = new List<int> { 101, 102 },
            notes = "Note campagne"
        };

        await vm.InitAsync(20, 0);

        Assert.Equal(20, vm.CampaignId);
        Assert.True(vm.IsEdit);
        Assert.False(vm.CanReadInvoice);
        Assert.Equal("Modifier la campagne", vm.PageTitle);
        Assert.Equal("Campagne Automne", vm.Name);
        Assert.Equal("instagram", vm.SelectedChannel?.Code);
        Assert.Equal(new DateTime(2026, 11, 1), vm.StartDate);
        Assert.Equal(new DateTime(2026, 11, 10), vm.EndDate);
        Assert.Equal("150,00", vm.AmountText);
        Assert.Equal(2, vm.TargetProducts.Count);
        Assert.Equal("2 articles ciblés", vm.TargetSummary);
        Assert.Equal("Note campagne", vm.Notes);
    }

    [Fact]
    public async Task CampaignForm_InitAsync_EditMode_404NotFound_SetsLoadError()
    {
        var vm = CreateFormViewModel();
        var apiError = ApiError.FromHttpResponse("""{ "error": "CAMPAIGN_NOT_FOUND" }""", HttpStatusCode.NotFound, null);
        _campaignService.GetException = new ApiException(apiError, HttpStatusCode.NotFound);

        await vm.InitAsync(25, 0);

        Assert.True(vm.HasLoadError);
        Assert.Equal("Cette campagne n'existe plus.", vm.LoadError);
    }

    [Fact]
    public async Task CampaignForm_InitAsync_Relance_CopiesParametersWithSlidingDatesAndRenames()
    {
        var vm = CreateFormViewModel();
        _campaignService.CampaignResult = new Campaign
        {
            id = 30,
            name = "Campagne Flash",
            channel = "tiktok",
            startDate = "2026-10-01",
            endDate = "2026-10-05", // 4 jours d'écart
            amount = 30000,
            currency = "CAD",
            productIds = new List<int> { 201 }
        };

        await vm.InitAsync(0, 0, fromCampaignId: 30);

        Assert.Equal(0, vm.CampaignId); // Nouvelle campagne
        Assert.False(vm.IsEdit);
        Assert.Equal("Campagne Flash (relance)", vm.Name);
        Assert.Equal("tiktok", vm.SelectedChannel?.Code);
        Assert.Equal(DateTime.Today, vm.StartDate);
        Assert.Equal(DateTime.Today.AddDays(4), vm.EndDate);
        Assert.Equal("300,00", vm.AmountText);
        Assert.Single(vm.TargetProducts);
        Assert.Equal(201, vm.TargetProducts[0].Id);
        Assert.False(vm.HasDocument);
    }

    [Fact]
    public async Task CampaignForm_UseDocumentAsync_PrefillsFromExtractedInvoice()
    {
        var vm = CreateFormViewModel();
        await vm.InitAsync(0, 0);

        _documentReaderService.DocumentResult = new GestionDocument
        {
            id = 40,
            type = "note",
            status = "extracted",
            extraction = JObject.Parse("""
            {
                "amount": 7500,
                "date": "2026-11-05",
                "currency": "usd",
                "merchant": "Google Ads",
                "description": "Publicité recherche",
                "confidence": { "overall": 0.8 },
                "notes": []
            }
            """)
        };

        await vm.UseDocumentAsync(40);

        Assert.Equal(40, vm.DocumentId);
        Assert.True(vm.HasDocument);
        Assert.False(vm.CanReadInvoice);
        Assert.Equal("Google Ads", vm.Name);
        Assert.Equal("google", vm.SelectedChannel?.Code);
        Assert.Equal("75,00", vm.AmountText);
        Assert.Equal(new DateTime(2026, 11, 5), vm.StartDate);
        Assert.Equal(new DateTime(2026, 11, 5), vm.EndDate);
        Assert.Equal("USD", vm.SelectedCurrency);
        Assert.True(vm.NeedsExchangeRate);
        Assert.True(vm.HasRemarks);
        Assert.Contains("Facture de la régie lue", vm.DocumentText);
    }

    [Theory]
    [InlineData("confirmed", "Cette facture est déjà enregistrée.")]
    [InlineData("expired", "Cette facture n'a pas été validée à temps : elle a été effacée. Lisez-la à nouveau.")]
    [InlineData("reading", "La lecture de la facture n'est pas terminée. Réessayez dans un instant.")]
    [InlineData("failed", "La lecture de la facture a échoué. Saisissez la campagne à la main.")]
    public async Task CampaignForm_UseDocumentAsync_NonExtractedStatus_ShowsAppropriateMessage(string status, string expectedMessage)
    {
        var vm = CreateFormViewModel();
        await vm.InitAsync(0, 0);

        _documentReaderService.DocumentResult = new GestionDocument
        {
            id = 45,
            type = "note",
            status = status
        };

        await vm.UseDocumentAsync(45);

        Assert.Equal(0, vm.DocumentId);
        Assert.False(vm.HasDocument);
        Assert.Contains(expectedMessage, Assert.Single(_dialogService.ShownInfos));
    }

    [Fact]
    public void CampaignForm_Targeting_ToggleAndRemove_UpdatesTargetProductsAndSummary()
    {
        var vm = CreateFormViewModel();
        Assert.Equal("Toutes les ventes du magasin", vm.TargetSummary);

        // Active le ciblage
        vm.IsTargeting = true;

        var p1 = new AvailableProduct { id = 10, name = "Chemise Blanche" };
        var p2 = new AvailableProduct { id = 20, name = "Polo Bleu" };
        vm.SearchResults.Add(p1);
        vm.SearchResults.Add(p2);

        // Coche p1
        vm.ToggleProductCommand.Execute(p1);
        Assert.Single(vm.TargetProducts);
        Assert.Equal(10, vm.TargetProducts[0].Id);
        Assert.True(p1.IsSelected);
        Assert.Equal("1 article ciblé", vm.TargetSummary);

        // Coche p2
        vm.ToggleProductCommand.Execute(p2);
        Assert.Equal(2, vm.TargetProducts.Count);
        Assert.True(p2.IsSelected);
        Assert.Equal("2 articles ciblés", vm.TargetSummary);

        // Retire la cible p1
        var target1 = vm.TargetProducts.First(t => t.Id == 10);
        vm.RemoveTargetCommand.Execute(target1);
        Assert.Single(vm.TargetProducts);
        Assert.Equal(20, vm.TargetProducts[0].Id);
        Assert.False(p1.IsSelected);
        Assert.Equal("1 article ciblé", vm.TargetSummary);

        // Désactive le ciblage : réinitialisation à tout le magasin
        vm.IsTargeting = false;
        Assert.Empty(vm.TargetProducts);
        Assert.Equal("Toutes les ventes du magasin", vm.TargetSummary);
    }

    [Fact]
    public async Task CampaignForm_Save_ValidationErrors_ShowsMissingFieldsDialog()
    {
        var vm = CreateFormViewModel();
        await vm.InitAsync(0, 0);

        // Formulaire incomplet : ni nom ni montant
        vm.Name = "";
        vm.AmountText = "";

        await vm.SaveCommand.ExecuteAsync(null);

        Assert.Contains("Donnez un nom à la campagne.", _dialogService.ShownInfos[0]);
        Assert.Contains("Montant invalide.", _dialogService.ShownInfos[0]);
        Assert.Contains("Choisissez le canal de la campagne.", _dialogService.ShownInfos[0]);
        Assert.Empty(_campaignService.CreateCalls);
        Assert.Empty(_campaignService.UpdateCalls);
    }

    [Fact]
    public async Task CampaignForm_Save_Create_SendsCreateRequest()
    {
        var vm = CreateFormViewModel();
        await vm.InitAsync(0, 0);

        vm.Name = "Nouvelle Campagne Facebook";
        vm.SelectedChannel = vm.Channels.First(c => c.Code == "facebook");
        vm.StartDate = new DateTime(2026, 11, 20);
        vm.EndDate = new DateTime(2026, 11, 25);
        vm.AmountText = "120,50";
        vm.SelectedCurrency = CampaignFormViewModel.SiteCurrencyChoice;
        vm.Notes = "Campagne promo";

        await vm.SaveCommand.ExecuteAsync(null);

        var body = Assert.Single(_campaignService.CreateCalls);
        Assert.Equal("Nouvelle Campagne Facebook", body["name"]!.Value<string>());
        Assert.Equal("facebook", body["channel"]!.Value<string>());
        Assert.Equal("2026-11-20", body["startDate"]!.Value<string>());
        Assert.Equal("2026-11-25", body["endDate"]!.Value<string>());
        Assert.Equal(12050, body["amount"]!.Value<long>());
        Assert.Equal("CAD", body["currency"]!.Value<string>());
        Assert.Equal("Campagne promo", body["notes"]!.Value<string>());

        Assert.Contains("Campagne enregistrée.", _dialogService.ShownInfos);
        Assert.Empty(_dialogService.ShownErrors);
    }

    [Fact]
    public async Task CampaignForm_Save_Edit_SendsUpdateRequest()
    {
        var vm = CreateFormViewModel();
        _campaignService.CampaignResult = new Campaign
        {
            id = 50,
            name = "Ancien Nom",
            channel = "other",
            startDate = "2026-11-01",
            endDate = "2026-11-05",
            amount = 10000,
            currency = "CAD"
        };
        await vm.InitAsync(50, 0);

        vm.Name = "Nom Modifié";
        vm.AmountText = "150,00";

        await vm.SaveCommand.ExecuteAsync(null);

        var (id, body) = Assert.Single(_campaignService.UpdateCalls);
        Assert.Equal(50, id);
        Assert.Equal("Nom Modifié", body["name"]!.Value<string>());
        Assert.Equal(15000, body["amount"]!.Value<long>());

        Assert.Contains("Campagne modifiée.", _dialogService.ShownInfos);
        Assert.Empty(_dialogService.ShownErrors);
    }

    [Fact]
    public async Task CampaignForm_Save_WithDocument_CallsConfirmDocumentAsync()
    {
        var vm = CreateFormViewModel();
        await vm.InitAsync(0, 0);

        _documentReaderService.DocumentResult = new GestionDocument
        {
            id = 60,
            type = "note",
            status = "extracted",
            extraction = JObject.Parse("""{ "amount": 8000, "date": "2026-11-10", "currency": "CAD", "merchant": "Instagram Ads" }""")
        };
        await vm.UseDocumentAsync(60);

        Assert.True(vm.HasDocument);

        await vm.SaveCommand.ExecuteAsync(null);

        // Confirme le document et n'appelle pas _campaignService.CreateAsync
        var (docId, body) = Assert.Single(_documentReaderService.ConfirmCalls);
        Assert.Equal(60, docId);
        var jobj = Assert.IsType<JObject>(body);
        Assert.Equal("campaign", jobj["as"]!.Value<string>());
        Assert.Equal(8000, jobj["amount"]!.Value<long>());
        Assert.Empty(_campaignService.CreateCalls);

        Assert.Contains("Campagne enregistrée.", _dialogService.ShownInfos);
        Assert.Empty(_dialogService.ShownErrors);
    }

    [Fact]
    public async Task CampaignForm_Save_ApiError_PeriodClosed_ShowsTranslatedMessage()
    {
        var vm = CreateFormViewModel();
        await vm.InitAsync(0, 0);

        vm.Name = "Campagne Décembre";
        vm.SelectedChannel = vm.Channels.First();
        vm.AmountText = "200,00";

        var apiError = ApiError.FromHttpResponse("""{ "error": "PERIOD_CLOSED" }""", HttpStatusCode.Conflict, null);
        _campaignService.CreateException = new ApiException(apiError, HttpStatusCode.Conflict);

        await vm.SaveCommand.ExecuteAsync(null);

        // Vérifie le message traduit exactement montré au gérant
        Assert.Contains("La période de la date de début est clôturée. Choisissez une autre date, ou rouvrez la période depuis le tableau de bord.", _dialogService.ShownInfos);
        Assert.Empty(_dialogService.ShownErrors);
    }
}
