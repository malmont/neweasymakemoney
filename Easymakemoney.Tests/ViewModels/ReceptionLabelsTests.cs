using Easymakemoney.Models;
using Easymakemoney.Services;
using Easymakemoney.Services.Api;
using Easymakemoney.Tests.Fakes;
using Easymakemoney.ViewModels.Lists;
using Newtonsoft.Json;
using Xunit;

namespace Easymakemoney.Tests.ViewModels;

/// <summary>Étiquettes d'une commande reçue (backend CB-2) : lecture, codes manquants, nombres modifiables, PDF.</summary>
public class ReceptionLabelsTests
{
    private const string Json = """
    { "commande": { "id": 9, "name": "Arrivage novembre", "receptionStatus": "received" },
      "format": { "code": "a4-50x30", "widthMm": 50, "heightMm": 30 }, "currency": "CAD", "pricesIncludeTax": false, "maxLabels": 500,
      "labels": [
        { "variantId": 40, "title": "Casquette", "subtitle": "Rouge / M", "barcode": "2900000000017", "barcodeOf": "variant", "symbology": "ean13",
          "priceCents": 2500, "priceIncludesTax": false, "quantityReceived": 3, "copies": 3, "printable": true, "reason": null },
        { "variantId": 41, "title": "Casquette", "subtitle": "Bleu / L", "barcode": null, "barcodeOf": null, "symbology": null,
          "priceCents": null, "priceIncludesTax": false, "quantityReceived": null, "copies": 5, "printable": false, "reason": "BARCODE_MISSING" } ],
      "missing": { "count": 1, "variantIds": [41] },
      "skipped": [ { "lineId": 77, "quantityReceived": 2, "reason": "NO_VARIANT" } ],
      "totals": { "lines": 3, "printable": 1, "labels": 8 } }
    """;

    private static ReceptionLabels Read() => JsonConvert.DeserializeObject<ReceptionLabels>(Json)!;

    [Fact]
    public void Model_ReadsLinesAndTexts()
    {
        var data = Read();

        Assert.True(data.HasMissing);
        Assert.Equal("Générer les codes manquants (1)", data.MissingButtonText);
        Assert.Equal("1 ligne sans variante n'a pas d'étiquette possible.", data.SkippedText);
        Assert.Equal(3, data.labels[0].Copies);
        Assert.Contains("3 reçu(s)", data.labels[0].DetailText);
        Assert.Equal("Pas encore de code-barres", data.labels[1].CodeText);
        Assert.Contains("quantité reçue inconnue · sans prix", data.labels[1].DetailText);
    }

    [Fact]
    public async Task Service_UrlsAndBody()
    {
        var http = new FakeHttpService();
        var service = new ReceptionLabelsService(http);

        await service.GetAsync(9);
        await service.GenerateMissingAsync(9);
        await service.PdfAsync(9, new[] { (40, 3) }, showPrice: true);

        Assert.Equal("commandes/9/etiquettes?locale=fr", http.RequestedUrls.Single());
        Assert.Equal("commandes/9/etiquettes/codes?locale=fr", http.PostedAuthData.Single().Url);
        Assert.Equal("commandes/9/etiquettes/pdf?locale=fr", http.PostedFileRequests.Single().Url);
        Assert.Equal("{\"labels\":[{\"variantId\":40,\"copies\":3}],\"showPrice\":true}", JsonConvert.SerializeObject(http.PostedFileRequests.Single().Data));
    }

    private sealed class FakeLabels : IReceptionLabelsService
    {
        public ReceptionLabels Data { get; set; } = Read();
        public ReceptionLabels AfterGenerate { get; set; } = new();
        public int GenerateCalls { get; private set; }
        public List<(int, List<(int, int)>, bool)> Pdfs { get; } = new();

        public Task<ReceptionLabels> GetAsync(int commandeId) => Task.FromResult(Data);
        public Task<ReceptionLabels> GenerateMissingAsync(int commandeId) { GenerateCalls++; return Task.FromResult(AfterGenerate); }

        public Task<DownloadedFile> PdfAsync(int commandeId, IEnumerable<(int VariantId, int Copies)> labels, bool showPrice)
        {
            Pdfs.Add((commandeId, labels.ToList(), showPrice));
            return Task.FromResult(new DownloadedFile(new byte[] { 1 }, "application/pdf", 3));
        }
    }

    private static (ReceptionLabelsViewModel Vm, FakeLabels Service, FakeDialogService Dialog, List<DownloadedFile> Opened) Create()
    {
        var service = new FakeLabels();
        var dialog = new FakeDialogService();
        var opened = new List<DownloadedFile>();
        var vm = new ReceptionLabelsViewModel(service, dialog) { CommandeId = 9 };
        vm.OpenFile = f => { opened.Add(f); return Task.CompletedTask; };
        return (vm, service, dialog, opened);
    }

    [Fact]
    public async Task Print_SendsOnlyPrintableLinesWithTheirCopies()
    {
        var (vm, service, _, opened) = Create();
        await vm.LoadAsync();
        vm.Labels[0].Copies = 2;
        vm.ShowPrice = true;

        await vm.PrintCommand.ExecuteAsync(null);

        var (id, lines, showPrice) = service.Pdfs.Single();
        Assert.Equal(9, id);
        Assert.Equal(new[] { (40, 2) }, lines);
        Assert.True(showPrice);
        Assert.Single(opened);
    }

    [Fact]
    public async Task Print_NothingPrintable_SaysToGenerateFirst()
    {
        var (vm, service, dialog, _) = Create();
        await vm.LoadAsync();
        vm.Labels[0].Copies = 0;

        await vm.PrintCommand.ExecuteAsync(null);

        Assert.Empty(service.Pdfs);
        Assert.Contains("générez d'abord les codes manquants", dialog.ShownErrors.Single().Message);
    }

    [Fact]
    public async Task Print_OverTheLimit_IsRefusedBeforeCallingTheServer()
    {
        var (vm, service, dialog, _) = Create();
        await vm.LoadAsync();
        vm.Labels[0].Copies = 501;

        await vm.PrintCommand.ExecuteAsync(null);

        Assert.Empty(service.Pdfs);
        Assert.Contains("500 au plus", dialog.ShownErrors.Single().Message);
    }

    [Fact]
    public async Task GenerateMissing_AsksThenShowsTheUpdatedList()
    {
        var (vm, service, dialog, _) = Create();
        await vm.LoadAsync();
        vm.Labels[0].Copies = 7;
        var updated = Read();
        updated.missing = new ReceptionLabelsMissing();
        updated.labels[1].barcode = "2900000000024";
        updated.labels[1].printable = true;
        updated.generated = new List<ReceptionGeneratedCode> { new() { variantId = 41, barcode = "2900000000024" } };
        service.AfterGenerate = updated;

        await vm.GenerateMissingCommand.ExecuteAsync(null);

        Assert.Equal(1, service.GenerateCalls);
        Assert.Single(dialog.ConfirmCalls);
        Assert.Equal("1 code-barres généré.", dialog.ShownInfos.Single());
        Assert.False(vm.Data!.HasMissing);
        Assert.True(vm.Labels[1].printable);
        Assert.Equal(7, vm.Labels[0].Copies);
    }

    [Fact]
    public async Task GenerateMissing_Refused_CallsNothing()
    {
        var (vm, service, dialog, _) = Create();
        await vm.LoadAsync();
        dialog.ConfirmResult = false;

        await vm.GenerateMissingCommand.ExecuteAsync(null);

        Assert.Equal(0, service.GenerateCalls);
    }
}
