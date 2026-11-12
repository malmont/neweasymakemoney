using System.Globalization;
using System.Net;
using Easymakemoney.Models;
using Easymakemoney.Models.Api;
using Easymakemoney.Services.Api;
using Easymakemoney.Tests.Fakes;
using Easymakemoney.Utilities;
using Easymakemoney.ViewModels.Lists;
using Xunit;

namespace Easymakemoney.Tests.ViewModels;

[Collection("SiteCurrency")]
public class PeriodBudgetViewModelTests : IDisposable
{
    private readonly CultureInfo _originalCulture;
    private readonly FakePeriodBudgetService _budgetService = new();
    private readonly FakeDialogService _dialogService = new();

    public PeriodBudgetViewModelTests()
    {
        _originalCulture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
        SiteCurrency.Reset();
        SiteCurrency.SetCurrency("CAD");
    }

    public void Dispose()
    {
        SiteCurrency.Reset();
        CultureInfo.CurrentCulture = _originalCulture;
    }

    private PeriodBudgetViewModel CreateViewModel()
    {
        return new PeriodBudgetViewModel(_budgetService, _dialogService)
        {
            PeriodId = 7
        };
    }

    private static PeriodBudget SampleBudget()
    {
        return new PeriodBudget
        {
            currency = "CAD",
            period = new PeriodBudgetPeriod { id = 7, name = "Automne 2026" },
            lines = new List<PeriodBudgetLine>
            {
                new() { category = "purchases", label = "Stock", planned = 300000, actual = 150000 },
                new() { category = "trip", label = "Voyage", planned = 50000, actual = 20000 }
            }
        };
    }

    [Fact]
    public async Task Budget_Load_SetsBudgetAndLines_WhenPeriodValid()
    {
        var vm = CreateViewModel();
        _budgetService.BudgetResult = SampleBudget();

        await vm.LoadAsync();

        Assert.Single(_budgetService.GetCalls);
        Assert.Equal(7, _budgetService.GetCalls[0]);
        Assert.NotNull(vm.Budget);
        Assert.Equal("Automne 2026", vm.PeriodName);
        Assert.Equal(2, vm.Lines.Count);
        Assert.True(vm.HasBudget);
        Assert.True(vm.CanEdit);
        Assert.False(vm.IsBusy);
    }

    [Fact]
    public async Task Budget_Load_WhenPeriodClosed_SetsReadOnly_CanEditFalse()
    {
        var vm = CreateViewModel();
        _budgetService.BudgetResult = SampleBudget();
        await vm.LoadAsync();

        Assert.True(vm.CanEdit);
        vm.ReadOnly = true;
        Assert.False(vm.CanEdit);
    }

    [Fact]
    public async Task Budget_EditLine_AmountEntered_CallsSavePlannedAsyncWithCents()
    {
        var vm = CreateViewModel();
        _budgetService.BudgetResult = SampleBudget();
        await vm.LoadAsync();

        var line = vm.Lines[1]; // category: "trip"
        vm.PromptAsync = (title, message, initial) => Task.FromResult<string?>("150,50");

        await vm.EditLineCommand.ExecuteAsync(line);

        var call = Assert.Single(_budgetService.SavePlannedCalls);
        Assert.Equal(7, call.PeriodId);
        Assert.Equal("trip", call.Category);
        Assert.Equal(15050L, call.PlannedCents);
    }

    [Fact]
    public async Task Budget_EditLine_Canceled_DoesNotCallSave()
    {
        var vm = CreateViewModel();
        _budgetService.BudgetResult = SampleBudget();
        await vm.LoadAsync();

        var line = vm.Lines[1];
        vm.PromptAsync = (title, message, initial) => Task.FromResult<string?>(null); // Annulé

        await vm.EditLineCommand.ExecuteAsync(line);

        Assert.Empty(_budgetService.SavePlannedCalls);
    }

    [Fact]
    public async Task Budget_EditLine_InvalidAmount_ShowsInfoDialogAndDoesNotCallSave()
    {
        var vm = CreateViewModel();
        _budgetService.BudgetResult = SampleBudget();
        await vm.LoadAsync();

        var line = vm.Lines[1];
        vm.PromptAsync = (title, message, initial) => Task.FromResult<string?>("montant_invalide");

        await vm.EditLineCommand.ExecuteAsync(line);

        Assert.Empty(_budgetService.SavePlannedCalls);
        Assert.Contains(_dialogService.ShownInfos, m => m.Contains("Montant invalide."));
    }

    [Fact]
    public async Task Budget_CopyLastYear_Confirmed_CallsCopyAsync()
    {
        var vm = CreateViewModel();
        _budgetService.BudgetResult = SampleBudget();
        await vm.LoadAsync();

        _dialogService.ConfirmResult = true;

        await vm.CopyLastYearCommand.ExecuteAsync(null);

        Assert.Single(_dialogService.ConfirmCalls);
        Assert.Equal("Reprendre l'an dernier", _dialogService.ConfirmCalls[0].Title);

        var call = Assert.Single(_budgetService.CopyCalls);
        Assert.Equal(7, call.PeriodId);
        Assert.Null(call.FromPeriodId);
    }

    [Fact]
    public async Task Budget_CopyLastYear_Declined_DoesNotCallCopy()
    {
        var vm = CreateViewModel();
        _budgetService.BudgetResult = SampleBudget();
        await vm.LoadAsync();

        _dialogService.ConfirmResult = false;

        await vm.CopyLastYearCommand.ExecuteAsync(null);

        Assert.Single(_dialogService.ConfirmCalls);
        Assert.Empty(_budgetService.CopyCalls);
    }

    [Fact]
    public async Task Budget_CopyLastYear_SourceNotFound_ShowsSpecificErrorMessage()
    {
        var vm = CreateViewModel();
        _budgetService.BudgetResult = SampleBudget();
        await vm.LoadAsync();

        var apiError = ApiError.FromHttpResponse(
            """{ "error": "BUDGET_SOURCE_NOT_FOUND", "message": "Aucun budget à reprendre pour la même période l'an dernier." }""",
            HttpStatusCode.NotFound, null);
        _budgetService.CopyException = new ApiException(apiError, HttpStatusCode.NotFound);
        _dialogService.ConfirmResult = true;

        await vm.CopyLastYearCommand.ExecuteAsync(null);

        var error = Assert.Single(_dialogService.ShownErrors);
        Assert.Contains("Aucun budget à reprendre pour la même période l'an dernier.", error.Message);
    }

    [Fact]
    public async Task Budget_Load_HttpError_ShowsDialogAndResetsBusy()
    {
        var vm = CreateViewModel();
        _budgetService.GetException = new Exception("Connexion perdue");

        await vm.LoadAsync();

        Assert.Single(_dialogService.ShownErrors);
        Assert.False(vm.IsBusy);
    }
}
