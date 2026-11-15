using Easymakemoney.Models;
using Easymakemoney.Services;
using Easymakemoney.Services.Api;
using Easymakemoney.Tests.Fakes;
using Easymakemoney.ViewModels.Lists;
using Newtonsoft.Json;
using Xunit;

namespace Easymakemoney.Tests.ViewModels;

/// <summary>« Mes rapports » du mobile (backend DOC-1 et DOC-2) : bilan de période ou d'année, suivi, ouverture, effacement.</summary>
public class ReportsTests
{
    private const string DoneJson = """
    {"id":2,"kind":"period","state":"done","title":"Bilan de la période Automne 2026","locale":"fr","params":{"periodId":8,"withAdvice":true},
     "createdAt":"2026-11-15T19:48:10+00:00","readyAt":"2026-11-15T19:48:24+00:00","expiresAt":"2027-02-13T19:48:10+00:00","pages":3,"size":31593,
     "download":{"url":"/api/gestion/rapports/2/fichier","filename":"rapport-bilan-de-la-periode-automne-2026-2.pdf","contentType":"application/pdf"},
     "advice":{"included":true},"error":null,"reused":false,"unitsCharged":1,"credits":{"monthly":1000,"used":19,"remaining":981,"resetAt":"2026-12-01T00:00:00-05:00"}}
    """;

    private static GestionReport Done() => JsonConvert.DeserializeObject<GestionReport>(DoneJson)!;

    [Fact]
    public void Model_ReadsTheServerExample()
    {
        var report = Done();

        Assert.True(report.IsDone);
        Assert.Equal("Bilan de période · 3 pages · 31 Ko · avec l'avis de l'IA", report.DetailText);
        Assert.StartsWith("Prêt, disponible jusqu'au", report.StateText);
        Assert.Contains("2027", report.StateText);
    }

    [Fact]
    public void Model_Failed_ShowsTheServerMessage()
    {
        var report = new GestionReport { state = "failed", error = new GestionReportError { code = "REPORT_TOO_LONG", message = "Ce rapport dépasse 40 pages : il n'a pas été fabriqué." } };

        Assert.Equal("Ce rapport dépasse 40 pages : il n'a pas été fabriqué.", report.StateText);
    }

    [Fact]
    public async Task Service_UrlsAndBodies()
    {
        var http = new FakeHttpService();
        var service = new GestionReportService(http);

        await service.RequestPeriodAsync(8, withAdvice: true);
        await service.RequestYearAsync(null, withAdvice: false);
        await service.RequestYearAsync(2025, withAdvice: false);
        await service.GetAsync(2);
        await service.ListAsync(1);
        await service.DownloadAsync(2);
        await service.DeleteAsync(2);

        Assert.Equal("gestion/rapports?locale=fr", http.PostedAuthData[0].Url);
        Assert.Equal("{\"kind\":\"period\",\"periodId\":8,\"withAdvice\":true,\"locale\":\"fr\"}", JsonConvert.SerializeObject(http.PostedAuthData[0].Data));
        Assert.Equal("{\"kind\":\"year\",\"withAdvice\":false,\"locale\":\"fr\"}", JsonConvert.SerializeObject(http.PostedAuthData[1].Data));
        Assert.Equal("{\"kind\":\"year\",\"withAdvice\":false,\"locale\":\"fr\",\"year\":2025}", JsonConvert.SerializeObject(http.PostedAuthData[2].Data));
        Assert.Equal(new[] { "gestion/rapports/2?locale=fr", "gestion/rapports?page=1&locale=fr" }, http.RequestedUrls);
        Assert.Equal("gestion/rapports/2/fichier?locale=fr", http.RequestedFiles.Single());
        Assert.Equal("gestion/rapports/2?locale=fr", http.DeletedUrls.Single());
    }

    private sealed class FakeReports : IGestionReportService
    {
        public List<(string Kind, int? Id, bool Advice)> Requests { get; } = new();
        public Queue<GestionReport> Reads { get; } = new();
        public GestionReport Requested { get; set; } = new() { id = 2, state = "pending" };
        public List<GestionReport> List { get; set; } = new();
        public List<int> Downloads { get; } = new();
        public List<int> Deletes { get; } = new();

        public Task<GestionReport> RequestPeriodAsync(int periodId, bool withAdvice) { Requests.Add(("period", periodId, withAdvice)); return Task.FromResult(Requested); }
        public Task<GestionReport> RequestYearAsync(int? year, bool withAdvice) { Requests.Add(("year", year, withAdvice)); return Task.FromResult(Requested); }
        public Task<GestionReport> GetAsync(int id) => Task.FromResult(Reads.Count > 0 ? Reads.Dequeue() : Requested);
        public Task<GestionReportList> ListAsync(int page) => Task.FromResult(new GestionReportList { reports = List });
        public Task<DownloadedFile> DownloadAsync(int id) { Downloads.Add(id); return Task.FromResult(new DownloadedFile(new byte[] { 1 }, "application/pdf", null)); }
        public Task DeleteAsync(int id) { Deletes.Add(id); return Task.CompletedTask; }
    }

    private static (ReportsViewModel Vm, FakeReports Service, FakeDialogService Dialog, List<string> Opened, List<int> Delays) Create()
    {
        var service = new FakeReports();
        var dialog = new FakeDialogService();
        var opened = new List<string>();
        var delays = new List<int>();
        var vm = new ReportsViewModel(service, dialog)
        {
            Delay = ms => { delays.Add(ms); return Task.CompletedTask; },
            OpenFile = (_, name) => { opened.Add(name); return Task.CompletedTask; },
        };
        return (vm, service, dialog, opened, delays);
    }

    [Fact]
    public async Task PeriodRequest_FromThePeriodScreen_AsksAdvice_PollsThenOpens()
    {
        var (vm, service, dialog, opened, delays) = Create();
        vm.PendingRequest = ("period", 8);
        service.Reads.Enqueue(new GestionReport { id = 2, state = "pending" });
        service.Reads.Enqueue(Done());

        await vm.LoadAsync();

        Assert.Equal(("period", (int?)8, true), service.Requests.Single());
        Assert.Contains("1 unité", dialog.ConfirmCalls.Single().Message);
        Assert.Equal(2, delays.Count);
        Assert.Equal(new[] { "rapport-bilan-de-la-periode-automne-2026-2.pdf" }, opened);
        Assert.Null(vm.PendingRequest);
    }

    [Fact]
    public async Task YearRequest_WithoutAdvice_AndReused_OpensAtOnce()
    {
        var (vm, service, dialog, opened, delays) = Create();
        dialog.ConfirmResult = false;
        service.Requested = Done();

        await vm.RequestCurrentYearCommand.ExecuteAsync(null);

        Assert.Equal(("year", (int?)null, false), service.Requests.Single());
        Assert.Empty(delays);
        Assert.Single(opened);
    }

    [Fact]
    public async Task Request_Failed_ShowsTheReason_AndOpensNothing()
    {
        var (vm, service, dialog, opened, _) = Create();
        service.Requested = new GestionReport { id = 3, state = "failed", title = "Bilan", error = new GestionReportError { message = "Ce rapport dépasse 40 pages : il n'a pas été fabriqué." } };

        await vm.RequestYearAsync(2025);

        Assert.Empty(opened);
        Assert.Contains("dépasse 40 pages", dialog.ShownInfos.Single());
    }

    [Fact]
    public async Task Request_StillPendingAfterTwoMinutes_SaysItWillAppear()
    {
        var (vm, service, dialog, opened, delays) = Create();

        await vm.RequestPeriodAsync(8);

        Assert.Equal(40, delays.Count);
        Assert.Empty(opened);
        Assert.Contains("encore en préparation", dialog.ShownInfos.Single());
    }

    [Fact]
    public async Task Delete_AsksFirst_AndRefusesWhilePending()
    {
        var (vm, service, dialog, _, _) = Create();

        await vm.DeleteCommand.ExecuteAsync(new GestionReport { id = 5, state = "pending", title = "x" });
        Assert.Empty(service.Deletes);
        Assert.Contains("en préparation", dialog.ShownErrors.Single().Message);

        dialog.ConfirmResults.Enqueue(false);
        await vm.DeleteCommand.ExecuteAsync(Done());
        Assert.Empty(service.Deletes);

        await vm.DeleteCommand.ExecuteAsync(Done());
        Assert.Equal(new[] { 2 }, service.Deletes);
    }

    [Fact]
    public async Task Open_DoneReport_DownloadsWithTheToken()
    {
        var (vm, service, _, opened, _) = Create();

        await vm.OpenCommand.ExecuteAsync(Done());

        Assert.Equal(new[] { 2 }, service.Downloads);
        Assert.Single(opened);
    }
}
