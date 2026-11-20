using System.Globalization;
using System.Net;
using Easymakemoney.Models;
using Easymakemoney.Models.Api;
using Easymakemoney.Services.Api;
using Easymakemoney.Tests.Fakes;
using Easymakemoney.Tests.Models;
using Easymakemoney.Utilities;
using Easymakemoney.ViewModels.Lists;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Easymakemoney.Tests.ViewModels;

/// <summary>Écrans des prêts (FIS-1) : liste, fiche, saisie avec aperçu en direct, lecture seule du directeur.</summary>
[Collection("SiteCurrency")]
public class LoanViewModelsTests : IDisposable
{
    private readonly CultureInfo _originalCulture = CultureInfo.CurrentCulture;
    private readonly FakeLoanService _service = new();
    private readonly FakeDialogService _dialog = new();

    public LoanViewModelsTests()
    {
        CultureInfo.CurrentCulture = new CultureInfo("fr-FR");
        SiteCurrency.Reset();
        SiteCurrency.SetCurrency("EUR");
    }

    public void Dispose()
    {
        CultureInfo.CurrentCulture = _originalCulture;
        SiteCurrency.Reset();
    }

    private static string Digits(string text) => new(text.Where(char.IsDigit).ToArray());

    private static Loan SampleLoan() => JsonConvert.DeserializeObject<Loan>(LoanTests.LoanJson)!;

    private static LoanPreview SamplePreview(long instalment)
    {
        var preview = JsonConvert.DeserializeObject<LoanPreview>(LoanTests.PreviewJson)!;
        foreach (var line in preview.schedule.Skip(1))
            line.total = instalment;
        return preview;
    }

    private static ApiException Forbidden() =>
        new(new ApiError { Code = "MODE_FORBIDDEN", Message = "payment" }, HttpStatusCode.Forbidden);

    private static ApiException Validation(string fieldsJson) =>
        new(new ApiError { Code = "VALIDATION_FAILED", Message = "Données non valides.", Body = JObject.Parse($$"""{ "error": "VALIDATION_FAILED", "fields": {{fieldsJson}} }""") },
            HttpStatusCode.UnprocessableEntity);

    private LoanEditViewModel CreateEdit(TimeSpan? delay = null)
    {
        var vm = new LoanEditViewModel(_service, _dialog) { PreviewDelay = delay ?? TimeSpan.Zero };
        vm.GoBackAsync = () =>
        {
            WentBack++;
            return Task.CompletedTask;
        };
        return vm;
    }

    private int WentBack { get; set; }

    private static void FillRequired(LoanEditViewModel vm)
    {
        vm.Label = "Aménagement";
        vm.AmountText = "20000";
        vm.RateText = "3,6";
        vm.DurationText = "36";
    }

    // Liste

    [Fact]
    public async Task List_ShowsRemainingTotalInSiteCurrency_AndNavigates()
    {
        _service.List = new LoanList
        {
            loans =
            {
                new Loan { id = 12, label = "Local", currency = "EUR", remainingToday = 545210 },
                new Loan { id = 13, label = "Dollars", currency = "USD", remainingToday = 999900 },
            },
        };
        var routes = new List<string>();
        var vm = new LoanListViewModel(_service, _dialog) { NavigateAsync = route => { routes.Add(route); return Task.CompletedTask; } };

        await vm.LoadAsync();
        await vm.AddCommand.ExecuteAsync(null);
        await vm.OpenCommand.ExecuteAsync(vm.Loans[0]);

        Assert.Equal(2, vm.Loans.Count);
        Assert.Equal("545210", Digits(vm.RemainingTotalText));
        Assert.True(vm.HasOtherCurrency);
        Assert.False(vm.IsEmpty);
        Assert.Equal(new[] { "LoanEditPage", "LoanDetailPage?LoanId=12" }, routes);
    }

    // Fiche

    [Fact]
    public async Task Detail_LoadsHeaderScheduleAndSortedPrepayments()
    {
        _service.Loan = SampleLoan();
        var vm = new LoanDetailViewModel(_service, _dialog);

        await vm.LoadAsync(12);

        Assert.Equal(12, vm.Loan!.id);
        Assert.Equal(3, vm.Schedule.Count);
        Assert.All(vm.Schedule, line => Assert.Equal("EUR", line.Currency));
        Assert.Equal(new[] { "2026-06-05", "2026-09-05" }, vm.Prepayments.Select(p => p.date));
        Assert.True(vm.HasPrepayments);
        Assert.True(vm.CanWrite);
    }

    [Fact]
    public async Task Detail_DeletePrepayment_IsConfirmed_AndShowsTheRecalculatedLoan()
    {
        _service.Loan = SampleLoan();
        var vm = new LoanDetailViewModel(_service, _dialog);
        await vm.LoadAsync(12);
        var recalculated = SampleLoan();
        recalculated.prepayments.RemoveAt(1);
        _service.Loan = recalculated;

        _dialog.ConfirmResult = false;
        await vm.DeletePrepaymentCommand.ExecuteAsync(vm.Prepayments[1]);
        Assert.Empty(_service.DeletedPrepayments);

        _dialog.ConfirmResult = true;
        await vm.DeletePrepaymentCommand.ExecuteAsync(vm.Prepayments[1]);

        Assert.Equal((12, 1), Assert.Single(_service.DeletedPrepayments));
        Assert.Single(vm.Prepayments);
    }

    [Fact]
    public async Task Detail_AddPrepayment_IsConfirmed_ThenReloads()
    {
        _service.Loan = SampleLoan();
        var vm = new LoanDetailViewModel(_service, _dialog);
        await vm.LoadAsync(12);
        vm.TogglePrepaymentFormCommand.Execute(null);
        vm.PrepaymentDate = new DateTime(2026, 10, 5);
        vm.PrepaymentAmountText = "1 500,50";
        vm.PrepaymentFeesText = "30";

        await vm.AddPrepaymentCommand.ExecuteAsync(null);

        var added = Assert.Single(_service.AddedPrepayments);
        Assert.Equal((12, new DateTime(2026, 10, 5), 150050L, (long?)3000), added);
        Assert.Contains("Remboursement anticipé", _dialog.ConfirmCalls[0].Title);
        Assert.Equal(new[] { 12, 12 }, _service.Fetched);
        Assert.False(vm.IsPrepaymentFormVisible);
    }

    [Fact]
    public async Task Detail_ForbiddenWrite_ShowsReadOnlyMessage_AndHidesWriteButtons()
    {
        _service.Loan = SampleLoan();
        var vm = new LoanDetailViewModel(_service, _dialog);
        await vm.LoadAsync(12);
        _service.WriteError = Forbidden();

        await vm.DeleteCommand.ExecuteAsync(null);

        Assert.True(vm.IsReadOnly);
        Assert.False(vm.CanWrite);
        Assert.Equal(LoanRules.ReadOnlyMessage, Assert.Single(_dialog.ShownInfos));
        Assert.Empty(_dialog.ShownErrors);
    }

    // Saisie et aperçu

    [Fact]
    public async Task Edit_PreviewWaitsForRequiredFields_ThenShowsInstalmentCostAndThreeLines()
    {
        var vm = CreateEdit();

        vm.Label = "Aménagement";
        await vm.PendingPreview;
        Assert.Empty(_service.Previews);
        Assert.Contains("le montant", vm.PreviewMessage);

        vm.AmountText = "20000";
        vm.RateText = "3,6";
        vm.DurationText = "36";
        var preview = Assert.Single(_service.Previews);
        Assert.Equal(2000000L, preview.Body["amount"]);
        Assert.Equal(36000L, preview.Body["ratePpm"]);
        Assert.False(preview.Body.ContainsKey("lender"));
        Assert.False(preview.Body.ContainsKey("notes"));

        preview.Answer.SetResult(SamplePreview(186000));
        await vm.PendingPreview;

        Assert.True(vm.HasPreview);
        Assert.StartsWith("Échéance : ", vm.PreviewInstalmentText);
        Assert.EndsWith("par trimestre", vm.PreviewInstalmentText);
        Assert.Equal("186000", Digits(vm.PreviewInstalmentText));
        Assert.Equal("112000", Digits(vm.PreviewCreditCostText));
        Assert.Equal(3, vm.PreviewLines.Count);
        Assert.Empty(_service.Created);
    }

    [Fact]
    public async Task Edit_OlderPreviewAnswer_NeverOverwritesANewerOne()
    {
        var vm = CreateEdit();
        FillRequired(vm);
        var firstRun = vm.PendingPreview;
        vm.RateText = "4";
        var secondRun = vm.PendingPreview;
        Assert.Equal(2, _service.Previews.Count);
        Assert.True(_service.Previews[0].Token.IsCancellationRequested);
        Assert.False(_service.Previews[1].Token.IsCancellationRequested);

        // La réponse récente arrive d'abord, l'ancienne ensuite.
        _service.Previews[1].Answer.SetResult(SamplePreview(190000));
        await secondRun;
        _service.Previews[0].Answer.SetResult(SamplePreview(186000));
        await firstRun;

        Assert.Equal("190000", Digits(vm.PreviewInstalmentText));
    }

    [Fact]
    public async Task Edit_PreviewIsDebounced_OnlyTheLastChangeIsSent()
    {
        var vm = CreateEdit(TimeSpan.FromMilliseconds(40));
        FillRequired(vm);
        vm.RateText = "3,7";
        vm.RateText = "3,8";

        for (var wait = 0; wait < 100 && _service.Previews.Count == 0; wait++)
            await Task.Delay(20);
        await Task.Delay(100);

        var preview = Assert.Single(_service.Previews);
        Assert.Equal(38000L, preview.Body["ratePpm"]);
        preview.Answer.SetResult(SamplePreview(186000));
        await vm.PendingPreview;
        Assert.True(vm.HasPreview);
    }

    [Fact]
    public async Task Edit_Preview422_ShowsErrorsUnderFields_WithoutDialog()
    {
        var vm = CreateEdit();
        FillRequired(vm);
        _service.Previews[^1].Answer.SetException(Validation("""{ "ratePpm": "LOAN_RATE_INVALID", "firstDueOn": "LOAN_DATE_INVALID" }"""));
        await vm.PendingPreview;

        Assert.True(vm.HasRateError);
        Assert.Equal(LoanRules.ErrorText("LOAN_RATE_INVALID"), vm.RateError);
        Assert.Equal(LoanRules.ErrorText("LOAN_DATE_INVALID"), vm.DatesError);
        Assert.False(vm.HasPreview);
        Assert.Empty(_dialog.ShownErrors);
        Assert.Empty(_dialog.ShownInfos);
    }

    [Fact]
    public async Task Edit_Create_SendsTheFormWithoutNulls()
    {
        var vm = CreateEdit();
        FillRequired(vm);
        vm.DisbursedOn = new DateTime(2026, 1, 10);
        vm.FirstDueOn = new DateTime(2026, 4, 10);
        vm.SelectedFrequency = vm.Frequencies.First(f => f.Code == "3");
        vm.HasDeferral = true;
        vm.DeferralMonthsText = "3";
        vm.SelectedInsuranceMode = vm.InsuranceModes.First(m => m.Code == "remaining");
        vm.InsuranceRateText = "0,3";
        vm.FeesText = "150";

        await vm.SaveCommand.ExecuteAsync(null);

        var body = Assert.Single(_service.Created);
        Assert.Equal("Aménagement", body["label"]);
        Assert.Equal(3, body["frequencyMonths"]);
        Assert.Equal("2026-01-10", body["disbursedOn"]);
        Assert.Equal("2026-04-10", body["firstDueOn"]);
        Assert.Equal(3, body["deferralMonths"]);
        Assert.Equal("partial", body["deferralKind"]);
        Assert.Equal("remaining", body["insuranceMode"]);
        Assert.Equal(3000L, body["insuranceRatePpm"]);
        Assert.Equal(15000L, body["fees"]);
        Assert.False(body.ContainsKey("insuranceAmount"));
        Assert.False(body.ContainsKey("lender"));
        Assert.Equal(1, WentBack);
    }

    [Fact]
    public async Task Edit_Update_SendsOnlyChangedFields()
    {
        var vm = CreateEdit();
        vm.Load(SampleLoan());
        Assert.Equal("4,5", vm.RateText);
        Assert.Equal("10000", vm.AmountText);
        Assert.Equal("Modifier le prêt", vm.PageTitle);

        vm.RateText = "3,9";
        vm.Lender = "";
        await vm.SaveCommand.ExecuteAsync(null);

        var (id, changes) = Assert.Single(_service.Updated);
        Assert.Equal(12, id);
        Assert.Equal(new[] { "ratePpm", "lender" }, changes.Keys);
        Assert.Equal(39000L, changes["ratePpm"]);
        Assert.Null(changes["lender"]);
        Assert.Empty(_service.Created);
    }

    [Fact]
    public async Task Edit_UnchangedLoan_SendsNothing()
    {
        var vm = CreateEdit();
        vm.Load(SampleLoan());

        await vm.SaveCommand.ExecuteAsync(null);

        Assert.Empty(_service.Updated);
        Assert.Equal(1, WentBack);
    }

    [Fact]
    public async Task Edit_Save422_ShowsErrorsUnderFields()
    {
        var vm = CreateEdit();
        FillRequired(vm);
        _service.WriteError = Validation("""{ "durationMonths": "LOAN_DURATION_INVALID" }""");

        await vm.SaveCommand.ExecuteAsync(null);

        Assert.Equal(LoanRules.ErrorText("LOAN_DURATION_INVALID"), vm.DurationError);
        Assert.Empty(_dialog.ShownErrors);
        Assert.Equal(0, WentBack);
    }

    [Fact]
    public async Task Edit_SaveLocalChecks_DoNotCallTheServer()
    {
        var vm = CreateEdit();

        await vm.SaveCommand.ExecuteAsync(null);

        Assert.True(vm.HasLabelError);
        Assert.True(vm.HasAmountError);
        Assert.True(vm.HasRateError);
        Assert.True(vm.HasDurationError);
        Assert.Empty(_service.Created);
    }

    [Fact]
    public async Task Edit_ForbiddenSave_ShowsReadOnlyMessage_AndHidesSave()
    {
        var vm = CreateEdit();
        FillRequired(vm);
        _service.WriteError = Forbidden();

        await vm.SaveCommand.ExecuteAsync(null);

        Assert.True(vm.IsReadOnly);
        Assert.False(vm.CanWrite);
        Assert.Equal(LoanRules.ReadOnlyMessage, Assert.Single(_dialog.ShownInfos));
        Assert.Equal(0, WentBack);
    }
}
