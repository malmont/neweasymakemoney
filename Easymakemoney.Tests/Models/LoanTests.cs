using System.Globalization;
using System.Net;
using System.Text;
using Easymakemoney.Models;
using Easymakemoney.Models.Api;
using Easymakemoney.Services;
using Easymakemoney.Services.Api;
using Easymakemoney.Tests.Fakes;
using Easymakemoney.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Easymakemoney.Tests.Models;

/// <summary>Prêts : contrat FIS-1 (fiche, résumé, échéancier, aperçu, refus 422, lecture seule du directeur).</summary>
[Collection("SiteCurrency")]
public class LoanTests : IDisposable
{
    private readonly CultureInfo _originalCulture = CultureInfo.CurrentCulture;

    public LoanTests()
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

    /// <summary>GET gestion/prets/{id} : fiche, résumé du jour et échéancier (ici tronqué à trois lignes).</summary>
    public const string LoanJson = """
    {
      "id": 12, "label": "Aménagement du local", "lender": "Banque du quartier", "amount": 1000000, "currency": "EUR",
      "ratePpm": 45000, "durationMonths": 12, "frequencyMonths": 1, "disbursedOn": "2026-01-05", "firstDueOn": "2026-02-05",
      "type": "annuity", "deferralMonths": 0, "deferralKind": null,
      "insuranceMode": "fixed", "insuranceAmount": 500, "insuranceRatePpm": null, "fees": 15000,
      "prepayments": [ { "date": "2026-06-05", "amount": 100000, "fees": 0 }, { "date": "2026-09-05", "amount": 200000, "fees": 3000 } ],
      "notes": "Garantie sur le fonds",
      "remainingToday": 545210,
      "nextInstalment": { "n": 11, "date": "2026-12-05", "periodStart": "2026-11-05", "capital": 54000, "interest": 1200, "insurance": 500, "total": 55700, "remaining": 491210, "deferral": null, "prepayment": null },
      "lastDueOn": "2027-01-05", "capitalized": 0,
      "totals": { "capital": 1000000, "interest": 21450, "insurance": 6000, "fees": 18000, "prepaymentFees": 3000, "creditCost": 45450, "paid": 1045450 },
      "schedule": [
        { "n": 1, "date": "2026-02-05", "periodStart": "2026-01-05", "capital": 81629, "interest": 3750, "insurance": 500, "total": 85879, "remaining": 918371, "deferral": null, "prepayment": null },
        { "n": 2, "date": "2026-03-05", "periodStart": "2026-02-05", "capital": 81935, "interest": 3444, "insurance": 500, "total": 85879, "remaining": 836436, "deferral": null, "prepayment": null },
        { "n": 5, "date": "2026-06-05", "periodStart": "2026-05-05", "capital": 82860, "interest": 2519, "insurance": 500, "total": 85879, "remaining": 488200, "deferral": null, "prepayment": 100000 }
      ]
    }
    """;

    /// <summary>POST gestion/prets/apercu : { loan, schedule, totals }, ici avec un différé partiel de trois mois.</summary>
    public const string PreviewJson = """
    {
      "loan": { "id": 0, "label": "Aperçu", "amount": 2000000, "currency": "EUR", "ratePpm": 36000, "durationMonths": 36, "frequencyMonths": 3,
                "disbursedOn": "2026-01-10", "firstDueOn": "2026-04-10", "type": "annuity", "deferralMonths": 3, "deferralKind": "partial",
                "insuranceMode": null, "fees": 0, "prepayments": [], "remainingToday": 2000000, "nextInstalment": "2026-04-10",
                "lastDueOn": "2029-01-10", "capitalized": 0 },
      "schedule": [
        { "n": 1, "date": "2026-04-10", "periodStart": "2026-01-10", "capital": 0, "interest": 18000, "insurance": 0, "total": 18000, "remaining": 2000000, "deferral": "partial", "prepayment": null },
        { "n": 2, "date": "2026-07-10", "periodStart": "2026-04-10", "capital": 168000, "interest": 18000, "insurance": 0, "total": 186000, "remaining": 1832000, "deferral": null, "prepayment": null },
        { "n": 3, "date": "2026-10-10", "periodStart": "2026-07-10", "capital": 169512, "interest": 16488, "insurance": 0, "total": 186000, "remaining": 1662488, "deferral": null, "prepayment": null },
        { "n": 4, "date": "2027-01-10", "periodStart": "2026-10-10", "capital": 171038, "interest": 14962, "insurance": 0, "total": 186000, "remaining": 1491450, "deferral": null, "prepayment": null }
      ],
      "totals": { "capital": 2000000, "interest": 112000, "insurance": 0, "fees": 0, "prepaymentFees": 0, "creditCost": 112000, "paid": 2112000 }
    }
    """;

    private static string Digits(string text) => new(text.Where(char.IsDigit).ToArray());

    [Theory]
    [InlineData(45000L, "4,5 %")]
    [InlineData(0L, "0 %")]
    [InlineData(300000L, "30 %")]
    [InlineData(45678L, "4,5678 %")]
    [InlineData(3600L, "0,36 %")]
    public void PercentText_ConvertsPpmToFrenchPercent(long ppm, string expected)
    {
        Assert.Equal(expected, LoanRules.PercentText(ppm));
    }

    [Theory]
    [InlineData("4,5", 45000L)]
    [InlineData("4.5", 45000L)]
    [InlineData("4,5 %", 45000L)]
    [InlineData("0", 0L)]
    [InlineData("30", 300000L)]
    [InlineData("0,36", 3600L)]
    [InlineData("4,5678", 45678L)]
    public void PercentToPpm_ReadsCommaOrDot(string text, long expected)
    {
        Assert.Equal(expected, LoanRules.PercentToPpm(text));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("-1")]
    public void PercentToPpm_RejectsUnreadableOrNegative(string? text)
    {
        Assert.Null(LoanRules.PercentToPpm(text));
    }

    [Fact]
    public void PercentRoundTrip_KeepsThePpm()
    {
        foreach (var ppm in new long[] { 1, 45000, 52500, 123456, 300000 })
            Assert.Equal(ppm, LoanRules.PercentToPpm(LoanRules.PercentInput(ppm)));
    }

    [Theory]
    [InlineData("LOAN_LABEL_INVALID", "nom")]
    [InlineData("LOAN_AMOUNT_INVALID", "montant")]
    [InlineData("LOAN_RATE_INVALID", "entre 0 et 30 %")]
    [InlineData("LOAN_DURATION_INVALID", "600 mois")]
    [InlineData("LOAN_FREQUENCY_INVALID", "mensuelles ou trimestrielles")]
    [InlineData("LOAN_DATE_INVALID", "La première échéance doit venir après le déblocage, au plus deux échéances plus tard.")]
    [InlineData("LOAN_TYPE_INVALID", "Capital constant")]
    [InlineData("LOAN_DEFERRAL_INVALID", "Différé")]
    [InlineData("LOAN_INSURANCE_INVALID", "Assurance")]
    [InlineData("LOAN_FEES_INVALID", "frais de dossier")]
    [InlineData("LOAN_PREPAYMENT_INVALID", "capital restant dû")]
    [InlineData("LOAN_CURRENCY_INVALID", "Devise")]
    public void ErrorText_GivesFrenchTextForEachValidationCode(string code, string expected)
    {
        var text = LoanRules.ErrorText(code);

        Assert.Contains(expected, text);
        Assert.NotEqual("Valeur refusée.", text);
    }

    [Fact]
    public void Labels_MatchTheContract()
    {
        Assert.Equal("Échéances constantes", LoanRules.TypeLabel("annuity"));
        Assert.Equal("Capital constant", LoanRules.TypeLabel("constant_capital"));
        Assert.Equal("mensuelle", LoanRules.FrequencyLabel(1));
        Assert.Equal("trimestrielle", LoanRules.FrequencyLabel(3));
        Assert.Equal("partiel (intérêts seuls)", LoanRules.DeferralLabel("partial"));
        Assert.Equal("total", LoanRules.DeferralLabel("total"));
        Assert.Equal(new[] { "none", "fixed", "initial", "remaining" }, LoanRules.InsuranceModes.Select(mode => mode.Code));
    }

    [Fact]
    public void RemainingTotal_CountsOnlyLoansInSiteCurrency()
    {
        var list = new LoanList
        {
            loans =
            {
                new Loan { id = 1, currency = "EUR", remainingToday = 500000 },
                new Loan { id = 2, currency = "usd", remainingToday = 900000 },
                new Loan { id = 3, currency = null, remainingToday = 120000 },
                new Loan { id = 4, currency = "eur", remainingToday = 30000 },
            },
        };

        Assert.Equal(650000, list.RemainingTotal("EUR"));
        Assert.True(list.HasOtherCurrency("EUR"));
        Assert.True(list.loans[1].IsOtherCurrency);
        Assert.Equal("USD · autre devise : hors trésorerie et résultats", list.loans[1].OtherCurrencyText);
        Assert.False(list.loans[0].IsOtherCurrency);
        Assert.Equal(string.Empty, list.loans[0].OtherCurrencyText);
    }

    [Fact]
    public void Loan_DeserializesFicheSummaryAndSchedule()
    {
        var loan = JsonConvert.DeserializeObject<Loan>(LoanJson)!;

        Assert.Equal(12, loan.id);
        Assert.Equal("Banque du quartier", loan.lender);
        Assert.Equal(1000000, loan.amount);
        Assert.Equal(45000, loan.ratePpm);
        Assert.Equal("fixed", loan.insuranceMode);
        Assert.Equal(500, loan.insuranceAmount);
        Assert.Null(loan.insuranceRatePpm);
        Assert.Equal(15000, loan.fees);
        Assert.Equal(545210, loan.remainingToday);
        Assert.Equal("2026-12-05", loan.nextInstalment!.date);
        Assert.Equal(55700, loan.nextInstalment.total);
        Assert.Equal("2027-01-05", loan.lastDueOn);
        Assert.Equal(45450, loan.totals.creditCost);
        Assert.Equal(1000000, loan.totals.capital);
        Assert.Equal(3, loan.schedule.Count);
        Assert.Equal(918371, loan.schedule[0].remaining);
        Assert.False(loan.schedule[0].IsDeferral);
        Assert.Equal(100000, loan.schedule[2].PrepaymentAmount);
        Assert.Contains("remboursement anticipé", loan.schedule[2].WithCurrency("EUR").NoteText);

        Assert.Equal("4,5 % · 12 mois · mensuelle · Échéances constantes", loan.TermsText);
        Assert.Equal("Débloqué le 05/01/2026 · première échéance le 05/02/2026 · dernière le 05/01/2027", loan.DatesText);
        Assert.StartsWith("Prochaine échéance : 05/12/2026 · ", loan.NextInstalmentLine);
        Assert.Equal("45450", Digits(loan.CreditCostText));
        Assert.Equal("1000000", Digits(loan.CapitalText));
    }

    [Fact]
    public void Loan_CreditCostIsShownApartFromCapital()
    {
        var loan = JsonConvert.DeserializeObject<Loan>(LoanJson)!;

        // Le coût du crédit ne contient jamais le capital : intérêts + assurance + frais.
        Assert.Equal(loan.totals.interest + loan.totals.insurance + loan.totals.fees, loan.totals.creditCost);
        Assert.Equal(loan.totals.capital + loan.totals.creditCost, loan.totals.paid);
        Assert.Contains("n'est pas une charge", LoanRules.CapitalNotChargeText);
    }

    [Fact]
    public void Prepayments_KeepTheServerOrder_WithTheirPositionAsIndex()
    {
        var loan = JsonConvert.DeserializeObject<Loan>(LoanJson)!;

        var sorted = loan.SortedPrepayments();

        Assert.Equal(new[] { "2026-06-05", "2026-09-05" }, sorted.Select(p => p.date));
        Assert.Equal(new[] { 0, 1 }, sorted.Select(p => p.Index));
        Assert.Equal("Sans frais", sorted[0].FeesText);
        Assert.StartsWith("Le 05/09/2026 · ", sorted[1].TitleText);
    }

    [Fact]
    public void Preview_DeserializesLoanScheduleAndTotals()
    {
        var preview = JsonConvert.DeserializeObject<LoanPreview>(PreviewJson)!;

        Assert.Equal(3, preview.loan.frequencyMonths);
        Assert.Equal("2026-04-10", preview.loan.nextInstalment!.date);
        Assert.Equal(4, preview.schedule.Count);
        Assert.True(preview.schedule[0].IsDeferral);
        Assert.Contains("différé", preview.schedule[0].WithCurrency("EUR").NoteText);
        Assert.Equal(112000, preview.totals.creditCost);

        // L'échéance courante est la première hors différé.
        Assert.Equal(2, preview.RegularInstalment()!.n);
        Assert.Equal(new[] { 1, 2, 3 }, preview.FirstLines().Select(line => line.n));
    }

    [Fact]
    public void FieldErrors_ReadsThe422FieldsByScreenZone()
    {
        var body = JObject.Parse("""{ "error": "VALIDATION_FAILED", "fields": { "ratePpm": "LOAN_RATE_INVALID", "firstDueOn": "LOAN_DATE_INVALID", "insuranceRatePpm": "LOAN_INSURANCE_INVALID", "currency": "LOAN_CURRENCY_INVALID" } }""");
        var exception = new ApiException(new ApiError { Code = "VALIDATION_FAILED", Message = "Données non valides.", Body = body }, HttpStatusCode.UnprocessableEntity);

        var errors = LoanRules.FieldErrors(exception);

        Assert.Equal(LoanRules.ErrorText("LOAN_RATE_INVALID"), errors["rate"]);
        Assert.Equal(LoanRules.ErrorText("LOAN_DATE_INVALID"), errors["dates"]);
        Assert.Equal(LoanRules.ErrorText("LOAN_INSURANCE_INVALID"), errors["insurance"]);
        Assert.Equal(LoanRules.ErrorText("LOAN_CURRENCY_INVALID"), errors["currency"]);
        Assert.Empty(LoanRules.FieldErrors(new InvalidOperationException()));
    }

    [Fact]
    public void IsReadOnlyRefusal_OnlyFor403ModeForbidden()
    {
        var forbidden = new ApiException(new ApiError { Code = "MODE_FORBIDDEN", Message = "payment" }, HttpStatusCode.Forbidden);
        var other = new ApiException(new ApiError { Code = "FORBIDDEN", Message = "Non." }, HttpStatusCode.Forbidden);

        Assert.True(LoanRules.IsReadOnlyRefusal(forbidden));
        Assert.False(LoanRules.IsReadOnlyRefusal(other));
        Assert.Equal("Lecture seule : un directeur de groupe ne peut pas modifier les prêts d'une boutique.", LoanRules.ReadOnlyMessage);
    }

    [Fact]
    public void IsLatestPreview_IgnoresAnOlderAnswer()
    {
        Assert.True(LoanRules.IsLatestPreview(3, 3));
        Assert.False(LoanRules.IsLatestPreview(2, 3));
    }

    [Fact]
    public void ChangedFields_KeepsOnlyWhatChanged()
    {
        var initial = new Dictionary<string, object?> { ["label"] = "Prêt", ["amount"] = 1000000L, ["ratePpm"] = 45000L, ["lender"] = "Banque", ["notes"] = null };
        var current = new Dictionary<string, object?> { ["label"] = "Prêt", ["amount"] = 1000000L, ["ratePpm"] = 39000L, ["lender"] = null, ["notes"] = null };

        var changed = LoanRules.ChangedFields(initial, current);

        Assert.Equal(new[] { "ratePpm", "lender" }, changed.Keys);
        Assert.Equal(39000L, changed["ratePpm"]);
        Assert.Null(changed["lender"]);
    }

    [Fact]
    public async Task Service_UsesTheLoanRoutes()
    {
        var http = new FakeHttpService { ResultToReturn = new Loan { id = 12 } };
        var service = new LoanService(http);

        await service.GetAsync(12);
        await service.UpdateAsync(12, new Dictionary<string, object?> { ["label"] = "Nouveau" });
        await service.AddPrepaymentAsync(12, new DateTime(2026, 9, 5), 200000, 3000);
        await service.AddPrepaymentAsync(12, new DateTime(2026, 10, 5), 100000, null);
        var recalculated = await service.DeletePrepaymentAsync(12, 1);
        await service.DeleteAsync(12);

        Assert.Equal(new[] { "gestion/prets/12", "gestion/prets/12" }, http.RequestedUrls);
        Assert.Equal(12, recalculated.id);
        Assert.Equal(new[] { "gestion/prets/12/remboursements/1", "gestion/prets/12" }, http.DeletedUrls);
        Assert.Equal("gestion/prets/12", http.PostedAuthData[0].Url);
        var first = Assert.IsType<Dictionary<string, object>>(http.PostedAuthData[1].Data);
        Assert.Equal("gestion/prets/12/remboursements", http.PostedAuthData[1].Url);
        Assert.Equal("2026-09-05", first["date"]);
        Assert.Equal(200000L, first["amount"]);
        Assert.Equal(3000L, first["fees"]);
        var second = Assert.IsType<Dictionary<string, object>>(http.PostedAuthData[2].Data);
        Assert.False(second.ContainsKey("fees"));

        http.ResultToReturn = new LoanPreview();
        await service.PreviewAsync(new Dictionary<string, object?>());
        Assert.Equal("gestion/prets/apercu", http.PostedAuthData[3].Url);

        http.ResultToReturn = new LoanList();
        await service.ListAsync();
        Assert.Equal("gestion/prets", http.RequestedUrls[^1]);
    }

    [Fact]
    public async Task HttpService_LoanCurrencyDoesNotChangeSiteCurrency()
    {
        var handler = new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{ "id": 3, "label": "Prêt en dollars", "currency": "USD", "ratePpm": 45000, "amount": 100000 }""", Encoding.UTF8, "application/json")
        });
        var service = new LoanService(new HttpService(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/api/") }));

        var loan = await service.GetAsync(3);

        Assert.Equal("USD", loan.currency);
        Assert.Equal("EUR", SiteCurrency.CurrentCurrency);
    }

    [Fact]
    public async Task HttpService_CancelledPreviewIsNotANetworkError()
    {
        var handler = new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var service = new LoanService(new HttpService(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/api/") }));
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var pending = service.PreviewAsync(new Dictionary<string, object?> { ["amount"] = 1000L }, cts.Token);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
    }
}
