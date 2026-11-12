using System.Globalization;
using System.Net;
using Easymakemoney.Models;
using Easymakemoney.Models.Api;
using Easymakemoney.Services.Api;
using Easymakemoney.Tests.Fakes;
using Easymakemoney.Utilities;
using Easymakemoney.ViewModels.Statistiques;
using Xunit;

namespace Easymakemoney.Tests.ViewModels;

[Collection("SiteCurrency")]
public class RentalPromotionViewModelTests : IDisposable
{
    private readonly CultureInfo _originalCulture = CultureInfo.CurrentCulture;
    private readonly FakeRentalPromotionService _service = new();
    private readonly FakeDialogService _dialogService = new();

    public RentalPromotionViewModelTests()
    {
        CultureInfo.CurrentCulture = new CultureInfo("fr-CA");
        SiteCurrency.Reset();
        RentalStatisticsViewModel.PromotionsChanged = false;
    }

    public void Dispose()
    {
        CultureInfo.CurrentCulture = _originalCulture;
        SiteCurrency.Reset();
        RentalStatisticsViewModel.PromotionsChanged = false;
    }

    private RentalPromotionViewModel CreateViewModel() =>
        new(_service, _dialogService);

    [Fact]
    public void Init_WithProduct_SetsScopeTextAndWeekdays()
    {
        var vm = CreateViewModel();

        // Initialisation pour 1 article spécifique
        vm.Init(0, 9, "Kayak de mer", 20, "6,7");

        Assert.Equal(0, vm.AdviceId);
        Assert.Equal(9, vm.ProductId);
        Assert.Contains("Kayak de mer", vm.ScopeText);
        Assert.Contains("la remise porte sur le tarif de location", vm.ScopeText);
        Assert.Equal("20", vm.PercentText);
        Assert.Equal(new[] { "6", "7" }, vm.Weekdays.Where(d => d.IsSelected).Select(d => d.Code));
    }

    [Fact]
    public void Init_WithAdvice_SetsScopeTextAndDays()
    {
        var vm = CreateViewModel();

        // Initialisation depuis un conseil du guide (jours 1 à 4 cochés)
        vm.Init(15, 0, null, 25, "1,2,3,4");

        Assert.Equal(15, vm.AdviceId);
        Assert.Equal(0, vm.ProductId);
        Assert.Equal("Articles du conseil du guide. Le conseil sera marqué fait ; son effet est mesuré à partir du début de la promotion.", vm.ScopeText);
        Assert.Equal("25", vm.PercentText);
        Assert.Equal(new[] { "1", "2", "3", "4" }, vm.Weekdays.Where(d => d.IsSelected).Select(d => d.Code));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("4")]     // < 5 %
    [InlineData("95")]    // > 90 %
    [InlineData("12.5")]  // non entier
    [InlineData("abc")]
    public async Task ComputePreview_InvalidPercent_ShowsInfoWithoutRequest(string invalidPercent)
    {
        var vm = CreateViewModel();
        vm.Init(0, 9, "Kayak", 20, "6,7");
        vm.PercentText = invalidPercent;

        await vm.ComputePreviewCommand.ExecuteAsync(null);

        Assert.Contains("La remise doit être un nombre entier entre 5 et 90 %.", _dialogService.ShownInfos);
        Assert.Empty(_service.PreviewCalls);
        Assert.Null(vm.Preview);
    }

    [Fact]
    public async Task ComputePreview_EndDateBeforeStartDate_ShowsInfoWithoutRequest()
    {
        var vm = CreateViewModel();
        vm.Init(0, 9, "Kayak", 20, "6,7");

        // Dates calculées à partir de DateTime.Today
        var today = DateTime.Today;
        vm.StartDate = today.AddDays(10);
        vm.EndDate = today.AddDays(5); // Fin avant début

        await vm.ComputePreviewCommand.ExecuteAsync(null);

        Assert.Contains("La date de fin doit suivre la date de début.", _dialogService.ShownInfos);
        Assert.Empty(_service.PreviewCalls);
        Assert.Null(vm.Preview);
    }

    [Fact]
    public async Task ComputePreview_ValidRequest_SendsProductOrAdviceAndWeekdays()
    {
        var today = DateTime.Today;
        var vm = CreateViewModel();
        vm.Init(0, 9, "Kayak", 20, "6,7");
        vm.StartDate = today.AddDays(1);
        vm.EndDate = today.AddDays(30);

        _service.PreviewResult = new RentalPromotionPreview
        {
            percent = 20,
            startDate = TreasuryDates.Iso(today.AddDays(1)),
            endDate = TreasuryDates.Iso(today.AddDays(30)),
            previewHash = "hash123",
            counts = new RentalPromotionCounts { ok = 1, overlap = 0, notRentable = 0 },
            items = new List<RentalPromotionPreviewItem>
            {
                new() { productId = 9, name = "Kayak", status = "ok", example = new RentalPromotionExample { durationType = "day", rate = 5000, discounted = 4000 } }
            }
        };

        await vm.ComputePreviewCommand.ExecuteAsync(null);

        var call = Assert.Single(_service.PreviewCalls);
        Assert.Equal(new List<int> { 9 }, call.ProductIds);
        Assert.Null(call.AdviceId);
        Assert.Equal(20, call.Percent);
        Assert.Equal(today.AddDays(1).Date, call.StartDate.Date);
        Assert.Equal(today.AddDays(30).Date, call.EndDate.Date);
        Assert.Equal(new List<int> { 6, 7 }, call.Weekdays);

        Assert.NotNull(vm.Preview);
        Assert.True(vm.HasPreview);
        Assert.True(vm.CanApply);
        var row = Assert.Single(vm.Rows);
        Assert.Equal("Kayak", row.Name);
        Assert.True(row.IsOk);
    }

    [Fact]
    public async Task ClearPreview_WhenInputsChange()
    {
        var today = DateTime.Today;
        var vm = CreateViewModel();
        vm.Init(0, 9, "Kayak", 20, "6,7");
        _service.PreviewResult = new RentalPromotionPreview
        {
            percent = 20,
            previewHash = "hash123",
            counts = new RentalPromotionCounts { ok = 1, overlap = 0, notRentable = 0 },
            items = new List<RentalPromotionPreviewItem>
            {
                new() { productId = 9, name = "Kayak", status = "ok" }
            }
        };

        await vm.ComputePreviewCommand.ExecuteAsync(null);
        Assert.True(vm.HasPreview);
        Assert.NotEmpty(vm.Rows);

        // Changement de pourcentage
        vm.PercentText = "30";
        Assert.False(vm.HasPreview);
        Assert.Empty(vm.Rows);

        // Remise d'un aperçu
        await vm.ComputePreviewCommand.ExecuteAsync(null);
        Assert.True(vm.HasPreview);

        // Changement de date de fin (partant de DateTime.Today)
        vm.EndDate = today.AddDays(40);
        Assert.False(vm.HasPreview);

        // Remise d'un aperçu
        await vm.ComputePreviewCommand.ExecuteAsync(null);
        Assert.True(vm.HasPreview);

        // Changement de jour
        vm.ToggleWeekdayCommand.Execute("1");
        Assert.False(vm.HasPreview);
    }

    [Fact]
    public void ToggleWeekday_KeepsAtLeastOneDaySelected()
    {
        var vm = CreateViewModel();
        vm.Init(0, 9, "Kayak", 20, "6"); // Seul le samedi est coché

        Assert.Single(vm.Weekdays, d => d.IsSelected);

        // Décocher le seul jour sélectionné ne doit rien faire
        vm.ToggleWeekdayCommand.Execute("6");

        Assert.True(vm.Weekdays.First(d => d.Code == "6").IsSelected);
    }

    [Fact]
    public async Task Apply_BlockedPreview_DoesNotPromptNorApply()
    {
        var vm = CreateViewModel();
        vm.Init(0, 9, "Kayak", 20, "6,7");

        // Aperçu bloqué par conflit de dates (overlap = 1)
        _service.PreviewResult = new RentalPromotionPreview
        {
            percent = 20,
            previewHash = "hash-overlap",
            counts = new RentalPromotionCounts { ok = 0, overlap = 1, notRentable = 0 },
            items = new List<RentalPromotionPreviewItem>
            {
                new() { productId = 9, name = "Kayak", status = "PROMOTION_OVERLAP" }
            }
        };

        await vm.ComputePreviewCommand.ExecuteAsync(null);
        Assert.False(vm.CanApply);

        await vm.ApplyCommand.ExecuteAsync(null);

        Assert.Empty(_dialogService.ConfirmCalls);
        Assert.Empty(_service.ApplyCalls);
        Assert.False(RentalStatisticsViewModel.PromotionsChanged);
    }

    [Fact]
    public async Task Apply_ConfirmationRefused_DoesNotSendRequest()
    {
        var vm = CreateViewModel();
        vm.Init(0, 9, "Kayak", 20, "6,7");

        _service.PreviewResult = new RentalPromotionPreview
        {
            percent = 20,
            previewHash = "hash123",
            counts = new RentalPromotionCounts { ok = 1, overlap = 0, notRentable = 0 },
            items = new List<RentalPromotionPreviewItem>
            {
                new() { productId = 9, name = "Kayak", status = "ok" }
            }
        };

        await vm.ComputePreviewCommand.ExecuteAsync(null);
        Assert.True(vm.CanApply);

        _dialogService.ConfirmResult = false; // Le gérant refuse
        await vm.ApplyCommand.ExecuteAsync(null);

        Assert.Single(_dialogService.ConfirmCalls);
        Assert.Empty(_service.ApplyCalls);
        Assert.False(RentalStatisticsViewModel.PromotionsChanged);
    }

    [Fact]
    public async Task Apply_Confirmed_SendsRequestAndSetsPromotionsChanged()
    {
        var vm = CreateViewModel();
        vm.Init(0, 9, "Kayak", 20, "6,7");

        _service.PreviewResult = new RentalPromotionPreview
        {
            percent = 20,
            previewHash = "hash-apply",
            counts = new RentalPromotionCounts { ok = 1, overlap = 0, notRentable = 0 },
            items = new List<RentalPromotionPreviewItem>
            {
                new() { productId = 9, name = "Kayak", status = "ok" }
            }
        };

        await vm.ComputePreviewCommand.ExecuteAsync(null);

        _dialogService.ConfirmResult = true;
        _service.ApplyResult = new RentalPromotionApplyResult
        {
            batch = new RentalPromotionBatch
            {
                id = 1,
                percent = 20,
                promotions = new List<RentalPromotionItem>
                {
                    new() { id = 10, batchId = 1, productId = 9, name = "Kayak" }
                }
            }
        };

        await vm.ApplyCommand.ExecuteAsync(null);

        var applyCall = Assert.Single(_service.ApplyCalls);
        Assert.Equal("hash-apply", applyCall.PreviewHash);
        Assert.Equal(new List<int> { 9 }, applyCall.Request.ProductIds);

        Assert.True(RentalStatisticsViewModel.PromotionsChanged);
        Assert.Contains("Promotion de location créée.", _dialogService.ShownInfos);
        Assert.Empty(_dialogService.ShownErrors);
    }

    [Theory]
    [InlineData("PRICES_CHANGED", "Les tarifs ont changé depuis l'aperçu. Vérifiez le nouvel aperçu.")]
    [InlineData("PROMOTION_OVERLAP", "Une promotion existe déjà sur ces jours pour un article. Vérifiez le nouvel aperçu.")]
    [InlineData("NOT_RENTABLE", "Un article ne se loue plus. Vérifiez le nouvel aperçu.")]
    public async Task Apply_ApiErrors_ShowsInfoAndRecomputesPreview(string errorCode, string expectedMessage)
    {
        var vm = CreateViewModel();
        vm.Init(0, 9, "Kayak", 20, "6,7");

        _service.PreviewResult = new RentalPromotionPreview
        {
            percent = 20,
            previewHash = "hash1",
            counts = new RentalPromotionCounts { ok = 1, overlap = 0, notRentable = 0 },
            items = new List<RentalPromotionPreviewItem>
            {
                new() { productId = 9, name = "Kayak", status = "ok" }
            }
        };

        await vm.ComputePreviewCommand.ExecuteAsync(null);
        Assert.Single(_service.PreviewCalls);

        var statusCode = errorCode == "NOT_RENTABLE" ? (HttpStatusCode)422 : HttpStatusCode.Conflict;
        var apiError = ApiError.FromHttpResponse($$"""{ "error": "{{errorCode}}" }""", statusCode, null);
        _service.ApplyException = new ApiException(apiError, statusCode);

        _dialogService.ConfirmResult = true;
        await vm.ApplyCommand.ExecuteAsync(null);

        Assert.Contains(expectedMessage, _dialogService.ShownInfos);
        Assert.Empty(_dialogService.ShownErrors);
        // ComputePreview est rappelé automatiquement suite à l'erreur
        Assert.Equal(2, _service.PreviewCalls.Count);
    }

    [Fact]
    public async Task Apply_OtherException_ShowsErrorDialog()
    {
        var vm = CreateViewModel();
        vm.Init(0, 9, "Kayak", 20, "6,7");

        _service.PreviewResult = new RentalPromotionPreview
        {
            percent = 20,
            previewHash = "hash1",
            counts = new RentalPromotionCounts { ok = 1, overlap = 0, notRentable = 0 },
            items = new List<RentalPromotionPreviewItem>
            {
                new() { productId = 9, name = "Kayak", status = "ok" }
            }
        };

        await vm.ComputePreviewCommand.ExecuteAsync(null);

        _service.ApplyException = new HttpRequestException("Réseau indisponible");
        _dialogService.ConfirmResult = true;

        await vm.ApplyCommand.ExecuteAsync(null);

        var error = Assert.Single(_dialogService.ShownErrors);
        Assert.IsType<HttpRequestException>(error);
    }
}
