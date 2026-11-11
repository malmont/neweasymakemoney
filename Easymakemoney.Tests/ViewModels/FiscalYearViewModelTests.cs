using System.Globalization;
using Easymakemoney.Models;
using Easymakemoney.Tests.Fakes;
using Easymakemoney.Utilities;
using Easymakemoney.ViewModels.Statistiques;
using Xunit;

namespace Easymakemoney.Tests.ViewModels;

[Collection("SiteCurrency")]
public class FiscalYearViewModelTests : IDisposable
{
    private readonly CultureInfo _originalCulture = CultureInfo.CurrentCulture;
    private readonly FakeYearStatisticsService _yearService = new();
    private readonly FakeDialogService _dialogService = new();

    public FiscalYearViewModelTests()
    {
        CultureInfo.CurrentCulture = new CultureInfo("fr-CA");
        SiteCurrency.Reset();
        YearStatisticsViewModel.FiscalYearChanged = false;
    }

    public void Dispose()
    {
        CultureInfo.CurrentCulture = _originalCulture;
        SiteCurrency.Reset();
        YearStatisticsViewModel.FiscalYearChanged = false;
    }

    private FiscalYearViewModel CreateViewModel() =>
        new(_yearService, _dialogService);

    [Fact]
    public async Task InitializeAsync_SelectsSavedMonth()
    {
        _yearService.FiscalYearToReturn = new FiscalYearSetting
        {
            startMonth = 4,
            current = new FiscalYearInfo
            {
                from = "2026-04-01",
                to = "2027-03-31",
                label = "Exercice 2026-2027"
            }
        };
        var vm = CreateViewModel();

        await vm.InitializeAsync();
        await vm.InitializeAsync(); // Second appel : ne recharge pas

        Assert.NotNull(vm.SelectedMonth);
        Assert.Equal("4", vm.SelectedMonth.Code);
        Assert.False(vm.IsChanged);
        Assert.Contains("Exercice 2026-2027", vm.CurrentText);
        Assert.NotEmpty(vm.PreviewText);
        Assert.NotEmpty(vm.AlignmentText);
        Assert.Single(_yearService.RequestedUrls);
        Assert.Equal("gestion/reglages/exercice", _yearService.RequestedUrls[0]);
        Assert.Empty(_dialogService.ShownErrors);
    }

    [Fact]
    public async Task IsChanged_DetectsMonthModification()
    {
        _yearService.FiscalYearToReturn = new FiscalYearSetting { startMonth = 4 };
        var vm = CreateViewModel();
        await vm.InitializeAsync();

        Assert.False(vm.IsChanged);

        // Choisir septembre (9)
        vm.SelectedMonth = vm.Months[8]; // mois 9 (index 8)
        Assert.Equal("9", vm.SelectedMonth.Code);
        Assert.True(vm.IsChanged);

        // Revenir à avril (4)
        vm.SelectedMonth = vm.Months[3]; // mois 4 (index 3)
        Assert.False(vm.IsChanged);
        Assert.Empty(_dialogService.ShownErrors);
    }

    [Fact]
    public async Task Save_WhenNotChanged_DoesNothing()
    {
        _yearService.FiscalYearToReturn = new FiscalYearSetting { startMonth = 4 };
        var vm = CreateViewModel();
        await vm.InitializeAsync();

        await vm.SaveCommand.ExecuteAsync(null);

        Assert.Empty(_dialogService.ShownInfos);
        Assert.Empty(_yearService.SaveFiscalYearCalls);
        Assert.False(YearStatisticsViewModel.FiscalYearChanged);
        Assert.Empty(_dialogService.ShownErrors);
    }

    [Fact]
    public async Task Save_WhenConfirmationRefused_DoesNotSendPut()
    {
        _yearService.FiscalYearToReturn = new FiscalYearSetting { startMonth = 4 };
        var vm = CreateViewModel();
        await vm.InitializeAsync();

        vm.SelectedMonth = vm.Months[8]; // mois 9
        _dialogService.ConfirmResult = false;

        await vm.SaveCommand.ExecuteAsync(null);

        Assert.Empty(_yearService.SaveFiscalYearCalls);
        Assert.False(YearStatisticsViewModel.FiscalYearChanged);
        Assert.True(vm.IsChanged);
        Assert.Empty(_dialogService.ShownErrors);
    }

    [Fact]
    public async Task Save_WhenConfirmationAccepted_SendsPutAndSetsFlagAndShowsConfirmation()
    {
        _yearService.FiscalYearToReturn = new FiscalYearSetting { startMonth = 4 };
        var vm = CreateViewModel();
        await vm.InitializeAsync();

        vm.SelectedMonth = vm.Months[8]; // mois 9
        _dialogService.ConfirmResult = true;

        await vm.SaveCommand.ExecuteAsync(null);

        Assert.Single(_yearService.SaveFiscalYearCalls);
        Assert.Equal(9, _yearService.SaveFiscalYearCalls[0]);
        Assert.True(YearStatisticsViewModel.FiscalYearChanged);
        Assert.Contains("L'exercice est enregistré.", _dialogService.ShownInfos);
        Assert.False(vm.IsChanged); // Repasse à faux après application de la réponse
        Assert.Empty(_dialogService.ShownErrors);
    }
}
