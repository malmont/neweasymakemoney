using System.Globalization;
using Easymakemoney.Models;
using Easymakemoney.Services;
using Easymakemoney.Tests.Fakes;
using Easymakemoney.Utilities;
using Easymakemoney.ViewModels.Lists;
using Xunit;

namespace Easymakemoney.Tests.ViewModels;

[Collection("SiteCurrency")]
public class AdviceViewModelTests : IDisposable
{
    private readonly CultureInfo _originalCulture = CultureInfo.CurrentCulture;
    private readonly FakeHttpService _httpService = new();
    private readonly FakeDialogService _dialogService = new();

    public AdviceViewModelTests()
    {
        CultureInfo.CurrentCulture = new CultureInfo("fr-CA");
        SiteCurrency.Reset();
    }

    public void Dispose()
    {
        CultureInfo.CurrentCulture = _originalCulture;
        SiteCurrency.Reset();
    }

    private AdviceViewModel CreateViewModel() =>
        new(new AdviceService(_httpService), _dialogService);

    [Fact]
    public void Advice_RentalLowWeekdays_CanCreatePromotion_AndIsRentalPromotion()
    {
        var rentalAdvice = new Advice
        {
            id = 42,
            kind = "RENTAL_LOW_WEEKDAYS",
            status = "new",
            active = true
        };

        // Un conseil de location RENTAL_LOW_WEEKDAYS peut créer une promotion et est bien une promotion de location
        Assert.True(rentalAdvice.CanCreatePromotion);
        Assert.True(rentalAdvice.IsRentalPromotion);

        // Par contraste, un conseil sur stock endormi peut créer une promotion, mais n'est pas une promotion de location
        var stockAdvice = new Advice
        {
            id = 43,
            kind = "SLOW_STOCK",
            status = "new",
            active = true
        };
        Assert.True(stockAdvice.CanCreatePromotion);
        Assert.False(stockAdvice.IsRentalPromotion);

        // Si le conseil est déjà fait ou inactif, CanCreatePromotion devient faux
        var doneRentalAdvice = new Advice
        {
            id = 44,
            kind = "RENTAL_LOW_WEEKDAYS",
            status = "done",
            active = true
        };
        Assert.False(doneRentalAdvice.CanCreatePromotion);
        Assert.True(doneRentalAdvice.IsRentalPromotion);
    }

    [Fact]
    public async Task CreatePromotionCommand_RentalLowWeekdays_ExecutesSafelyWithoutShell()
    {
        var vm = CreateViewModel();
        var rentalAdvice = new Advice
        {
            id = 42,
            kind = "RENTAL_LOW_WEEKDAYS",
            status = "new",
            active = true
        };

        // Shell.Current est null dans l'environnement xUnit headless.
        // La méthode retourne immédiatement grâce à "if (advice is not { CanCreatePromotion: true } || Shell.Current is not { } shell) return;".
        // Ce test confirme l'absence de plantage sans Shell applicatif.
        var exception = await Record.ExceptionAsync(() => vm.CreatePromotionCommand.ExecuteAsync(rentalAdvice));

        Assert.Null(exception);
        Assert.Empty(_dialogService.ShownErrors);
    }

    [Fact]
    public async Task CreatePromotionCommand_NullOrCannotCreate_ExecutesSafely()
    {
        var vm = CreateViewModel();

        var exceptionNull = await Record.ExceptionAsync(() => vm.CreatePromotionCommand.ExecuteAsync(null));
        Assert.Null(exceptionNull);

        var cannotCreate = new Advice { id = 45, kind = "CASH_TIGHT", status = "new" };
        var exceptionCannotCreate = await Record.ExceptionAsync(() => vm.CreatePromotionCommand.ExecuteAsync(cannotCreate));
        Assert.Null(exceptionCannotCreate);
        Assert.Empty(_dialogService.ShownErrors);
    }
}

