using System.Net;
using Easymakemoney.Models;
using Easymakemoney.Models.Api;
using Easymakemoney.Services;
using Easymakemoney.Services.Api;
using Easymakemoney.Tests.Fakes;
using Easymakemoney.ViewModels.Lists;
using Newtonsoft.Json;
using Xunit;

namespace Easymakemoney.Tests.ViewModels;

/// <summary>Écran « Codes-barres » du mobile (backend CB-3) : recherche, pose avec confirmation, génération, retrait, étiquettes.</summary>
public class BarcodeTests
{
    private const string FoundJson = """
    { "code": "2900000000017", "found": true, "refusal": null, "currency": "CAD",
      "sheet": { "type": "variant", "productId": 12, "variantId": 40, "name": "Casquette", "variantLabel": "Rouge / M",
                 "barcode": "2900000000017", "symbology": "ean13", "isPos": true, "isWeb": false, "stock": 3,
                 "priceCents": 2500, "priceIncludesTax": false } }
    """;

    private static BarcodeSheet Sheet(string? barcode = "2900000000017", bool variant = true) => new()
    {
        type = variant ? "variant" : "product", productId = 12, variantId = variant ? 40 : null, name = "Casquette",
        variantLabel = variant ? "Rouge / M" : null, barcode = barcode, isPos = true, stock = 3, priceCents = 2500,
    };

    private static ApiException Error(string code, string body) =>
        new(ApiError.FromHttpResponse(body, HttpStatusCode.Conflict, null), HttpStatusCode.Conflict);

    // --- Modèles ---

    [Fact]
    public void Search_Found_ReadsSheetAndTexts()
    {
        var search = JsonConvert.DeserializeObject<BarcodeSearch>(FoundJson)!;

        Assert.True(search.IsFound);
        Assert.False(search.CanAssign);
        Assert.Equal("Casquette · Rouge / M", search.sheet!.Title);
        Assert.Equal("Variante", search.sheet.KindText);
        Assert.Equal(40, search.sheet.SheetId);
        Assert.Equal("Stock : 3", search.sheet.StockText);
        Assert.Equal("Vendu en caisse seulement", search.sheet.ChannelsText);
        Assert.EndsWith("hors taxes", search.sheet.PriceText);
        Assert.False(search.HasStatus);
    }

    [Theory]
    [InlineData(null, true, "n'est porté par aucun article")]
    [InlineData("BARCODE_RESERVED", false, "réservé aux tickets")]
    [InlineData("BARCODE_INVALID", false, "forme acceptée")]
    public void Search_NotFound_SaysWhyAndWhetherItCanBePosed(string? refusal, bool canAssign, string expected)
    {
        var search = new BarcodeSearch { code = "ABC", found = false, refusal = refusal };

        Assert.Equal(canAssign, search.CanAssign);
        Assert.Contains(expected, search.StatusText);
    }

    [Fact]
    public void Sheet_ProductWithoutVariant_UsesProductId_AndNoPrice()
    {
        var sheet = new BarcodeSheet { type = "product", productId = 7, name = "Sac", priceCents = null };

        Assert.False(sheet.IsVariant);
        Assert.Equal(7, sheet.SheetId);
        Assert.Equal("Sac", sheet.Title);
        Assert.Equal("Pas de prix", sheet.PriceText);
        Assert.Equal("Pas de code-barres", sheet.CodeText);
    }

    [Fact]
    public void Missing_Text()
    {
        Assert.Equal("Toutes les fiches ont un code-barres.", new BarcodeMissing().Text);
        Assert.Contains("12 fiches sans code-barres", new BarcodeMissing { total = 12, variants = 10, products = 2 }.Text);
    }

    // --- Service ---

    [Fact]
    public async Task Service_UrlsAndBodies()
    {
        var http = new FakeHttpService();
        var service = new CatalogBarcodeService(http);

        await service.SearchAsync(" 29 00/1 ");
        await service.GetMissingAsync();
        await service.GenerateMissingAsync();
        await service.GenerateAsync(variant: true, 40);
        await service.GenerateAsync(variant: false, 7);
        await service.SetAsync(variant: true, 40, " ABC ", replace: false);
        await service.SetAsync(variant: false, 7, "", replace: true);
        await service.LabelsPdfAsync(new[] { new BarcodeLabelRequest(Sheet(), 3), new BarcodeLabelRequest(Sheet(variant: false), 1) }, showPrice: true);

        Assert.Equal("gestion/codes-barres/recherche?code=29%2000%2F1&locale=fr", http.RequestedUrls[0]);
        Assert.Equal("gestion/codes-barres/manquants?locale=fr", http.RequestedUrls[1]);
        Assert.Equal("gestion/codes-barres/manquants/generer?locale=fr", http.PostedAuthData[0].Url);
        Assert.Equal("gestion/codes-barres/variantes/40/generer?locale=fr", http.PostedAuthData[1].Url);
        Assert.Equal("gestion/codes-barres/articles/7/generer?locale=fr", http.PostedAuthData[2].Url);

        Assert.Equal("gestion/codes-barres/variantes/40?locale=fr", http.PutData[0].Url);
        Assert.Equal("{\"barcode\":\"ABC\"}", JsonConvert.SerializeObject(http.PutData[0].Data));
        Assert.Equal("gestion/codes-barres/articles/7?locale=fr", http.PutData[1].Url);
        Assert.Equal("{\"barcode\":null,\"replace\":true}", JsonConvert.SerializeObject(http.PutData[1].Data));

        Assert.Equal("gestion/codes-barres/etiquettes/pdf?locale=fr", http.PostedFileRequests[0].Url);
        Assert.Equal("{\"labels\":[{\"copies\":3,\"variantId\":40},{\"copies\":1,\"productId\":12}],\"showPrice\":true,\"format\":\"a4-50x30\"}",
            JsonConvert.SerializeObject(http.PostedFileRequests[0].Data));
    }

    [Fact]
    public void ApiError_KeepsBody_ForRouteFields()
    {
        var error = ApiError.FromHttpResponse("{\"code\":\"BARCODE_ALREADY_SET\",\"message\":\"Déjà un code.\",\"current\":\"OLD-1\"}", HttpStatusCode.Conflict, null);

        Assert.Equal("BARCODE_ALREADY_SET", error.Code);
        Assert.Equal("OLD-1", error.Body?["current"]?.ToString());
    }

    // --- Écran ---

    private sealed class Harness
    {
        public FakeBarcodeService Service { get; } = new();
        public FakeProductAttachmentService Products { get; } = new();
        public FakeListProductVariantService Variants { get; } = new();
        public FakeScanner Scanner { get; } = new();
        public FakeDialogService Dialog { get; } = new();
        public List<DownloadedFile> Opened { get; } = new();
        public BarcodeViewModel Vm { get; }

        public Harness()
        {
            Vm = new BarcodeViewModel(Service, Products, Variants, Scanner, Dialog);
            Vm.OpenFile = f => { Opened.Add(f); return Task.CompletedTask; };
        }
    }

    [Fact]
    public async Task Scan_SearchesTheReadCode_AndShowsTheSheet()
    {
        var h = new Harness();
        h.Scanner.Next = "2900000000017";
        h.Service.SearchResult = JsonConvert.DeserializeObject<BarcodeSearch>(FoundJson)!;

        await h.Vm.ScanCommand.ExecuteAsync(null);

        Assert.Equal(new[] { "2900000000017" }, h.Service.Searches);
        Assert.True(h.Vm.HasSheet);
        Assert.Equal("Casquette · Rouge / M", h.Vm.Sheet!.Title);
    }

    [Fact]
    public async Task Search_Blank_DoesNotCallTheServer()
    {
        var h = new Harness();
        h.Vm.Code = "   ";

        await h.Vm.SearchCommand.ExecuteAsync(null);

        Assert.Empty(h.Service.Searches);
    }

    [Fact]
    public async Task Assign_FreeCode_PicksProductThenVariant_AndPutsWithoutReplace()
    {
        var h = new Harness();
        h.Vm.Code = "3017620422003";
        h.Service.SearchResult = new BarcodeSearch { code = "3017620422003", found = false };
        await h.Vm.SearchCommand.ExecuteAsync(null);
        Assert.True(h.Vm.CanAssign);

        h.Dialog.PromptResult = "casq";
        h.Products.AvailableProductsResult = new AvailableProductsPage { items = { new AvailableProduct { id = 12, name = "Casquette" } } };
        h.Variants.Variants.Add(new ProductVariant { id = 40, color = new ColorsProduct { name = "Rouge", codeHexa = "#f00" }, size = new SizeProduct { name = "M" } });
        h.Dialog.ChooseResults.Enqueue("Casquette");
        h.Dialog.ChooseResults.Enqueue("Rouge / M");
        h.Service.WriteResult = new BarcodeWrite { barcode = "3017620422003", changed = true, sheet = Sheet("3017620422003") };

        await h.Vm.AssignCommand.ExecuteAsync(null);

        Assert.Equal(("casq", 1, 20, true), h.Products.GetAvailableCalls[0]);
        Assert.Equal(new[] { (true, 40, (string?)"3017620422003", false) }, h.Service.Writes);
        Assert.True(h.Vm.HasSheet);
        Assert.Empty(h.Dialog.ShownErrors);
    }

    [Fact]
    public async Task Assign_SheetAlreadyHasACode_AsksBeforeReplacing()
    {
        var h = new Harness();
        h.Vm.Code = "NEW-1";
        h.Service.SearchResult = new BarcodeSearch { code = "NEW-1", found = false };
        await h.Vm.SearchCommand.ExecuteAsync(null);

        h.Dialog.PromptResult = "sac";
        h.Products.AvailableProductsResult = new AvailableProductsPage { items = { new AvailableProduct { id = 7, name = "Sac" } } };
        h.Dialog.ChooseResults.Enqueue("Sac");
        h.Service.WriteErrors.Enqueue(Error("BARCODE_ALREADY_SET", "{\"code\":\"BARCODE_ALREADY_SET\",\"current\":\"OLD-1\"}"));
        h.Service.WriteResult = new BarcodeWrite { barcode = "NEW-1", sheet = Sheet("NEW-1", variant: false) };

        await h.Vm.AssignCommand.ExecuteAsync(null);

        Assert.Contains("OLD-1", h.Dialog.ConfirmCalls.Single().Message);
        Assert.Equal(new[] { (false, 7, (string?)"NEW-1", false), (false, 7, (string?)"NEW-1", true) }, h.Service.Writes);
    }

    [Fact]
    public async Task Assign_ReplaceRefused_WritesNothingMore()
    {
        var h = new Harness();
        h.Vm.Code = "NEW-1";
        h.Service.SearchResult = new BarcodeSearch { code = "NEW-1", found = false };
        await h.Vm.SearchCommand.ExecuteAsync(null);
        h.Dialog.PromptResult = "sac";
        h.Products.AvailableProductsResult = new AvailableProductsPage { items = { new AvailableProduct { id = 7, name = "Sac" } } };
        h.Dialog.ChooseResults.Enqueue("Sac");
        h.Service.WriteErrors.Enqueue(Error("BARCODE_ALREADY_SET", "{\"code\":\"BARCODE_ALREADY_SET\",\"current\":\"OLD-1\"}"));
        h.Dialog.ConfirmResult = false;

        await h.Vm.AssignCommand.ExecuteAsync(null);

        Assert.Single(h.Service.Writes);
        Assert.Empty(h.Dialog.ShownErrors);
    }

    [Fact]
    public async Task Write_CheckDigitWarning_IsShown()
    {
        var h = new Harness();
        h.Service.SearchResult = JsonConvert.DeserializeObject<BarcodeSearch>(FoundJson)!;
        h.Vm.Code = "x";
        await h.Vm.SearchCommand.ExecuteAsync(null);
        h.Dialog.ChooseResult = "Taper le code";
        h.Dialog.PromptResult = "1234567890123";
        h.Service.WriteResult = new BarcodeWrite { barcode = "1234567890123", sheet = Sheet("1234567890123"),
            warning = new BarcodeWarning { code = "BARCODE_CHECK_DIGIT", message = "La clé de contrôle ne correspond pas." } };

        await h.Vm.ReplaceCommand.ExecuteAsync(null);

        Assert.Equal((true, 40, (string?)"1234567890123", false), h.Service.Writes.Single());
        Assert.Contains("La clé de contrôle ne correspond pas.", h.Dialog.ShownInfos);
    }

    [Fact]
    public async Task Remove_AsksFirst_ThenSendsEmptyWithReplace()
    {
        var h = new Harness();
        h.Service.SearchResult = JsonConvert.DeserializeObject<BarcodeSearch>(FoundJson)!;
        h.Vm.Code = "x";
        await h.Vm.SearchCommand.ExecuteAsync(null);

        h.Dialog.ConfirmResults.Enqueue(false);
        await h.Vm.RemoveCommand.ExecuteAsync(null);
        Assert.Empty(h.Service.Writes);

        await h.Vm.RemoveCommand.ExecuteAsync(null);
        Assert.Equal((true, 40, (string?)null, true), h.Service.Writes.Single());
        Assert.False(h.Vm.HasSheet);
    }

    [Fact]
    public async Task GenerateMissing_LoopsUntilNothingRemains_AndStopsWhenNothingIsWritten()
    {
        var h = new Harness();
        h.Service.Missing = new BarcodeMissing { total = 1200, variants = 1200 };
        h.Service.GenerateRuns.Enqueue(new BarcodeGenerateMissing { written = 500, remaining = 700 });
        h.Service.GenerateRuns.Enqueue(new BarcodeGenerateMissing { written = 500, remaining = 200 });
        h.Service.GenerateRuns.Enqueue(new BarcodeGenerateMissing { written = 0, remaining = 200 });
        h.Service.GenerateRuns.Enqueue(new BarcodeGenerateMissing { written = 999, remaining = 0 });

        await h.Vm.GenerateMissingCommand.ExecuteAsync(null);

        Assert.Equal(3, h.Service.GenerateCalls);
        var info = h.Dialog.ShownInfos.Single();
        Assert.StartsWith("1000 codes-barres générés.", info);
        Assert.Contains("200 fiche(s) occupée(s)", info);
    }

    [Fact]
    public async Task GenerateMissing_NothingMissing_DoesNotAsk()
    {
        var h = new Harness();
        h.Service.Missing = new BarcodeMissing();

        await h.Vm.GenerateMissingCommand.ExecuteAsync(null);

        Assert.Empty(h.Dialog.ConfirmCalls);
        Assert.Equal(0, h.Service.GenerateCalls);
    }

    [Fact]
    public async Task PrintLabel_AsksCopiesAndPrice_ThenOpensThePdf()
    {
        var h = new Harness();
        h.Service.SearchResult = JsonConvert.DeserializeObject<BarcodeSearch>(FoundJson)!;
        h.Vm.Code = "x";
        await h.Vm.SearchCommand.ExecuteAsync(null);
        h.Dialog.PromptResult = "4";
        h.Dialog.ConfirmResult = false;
        h.Dialog.ChooseResult = "Rouleau thermique";

        await h.Vm.PrintLabelCommand.ExecuteAsync(null);

        Assert.Equal("roll-50x30", h.Service.Formats.Single());

        var (labels, showPrice) = h.Service.LabelRequests.Single();
        Assert.Equal(4, labels.Single().Copies);
        Assert.False(showPrice);
        Assert.Single(h.Opened);
    }

    [Fact]
    public async Task PrintLabel_BadCount_IsRefusedInFrench()
    {
        var h = new Harness();
        h.Service.SearchResult = JsonConvert.DeserializeObject<BarcodeSearch>(FoundJson)!;
        h.Vm.Code = "x";
        await h.Vm.SearchCommand.ExecuteAsync(null);
        h.Dialog.PromptResult = "0";

        await h.Vm.PrintLabelCommand.ExecuteAsync(null);

        Assert.Empty(h.Service.LabelRequests);
        Assert.Contains("entre 1 et 500", h.Dialog.ShownErrors.Single().Message);
    }

    [Fact]
    public async Task GenerateForSheet_ProductWithoutVariant_GeneratesOnTheProduct()
    {
        var h = new Harness();
        h.Dialog.PromptResult = "sac";
        h.Products.AvailableProductsResult = new AvailableProductsPage { items = { new AvailableProduct { id = 7, name = "Sac" } } };
        h.Dialog.ChooseResult = "Sac";
        h.Service.Generated = new BarcodeGenerated { barcode = "2900000000024", sheet = Sheet("2900000000024", variant: false) };

        await h.Vm.GenerateForSheetCommand.ExecuteAsync(null);

        Assert.Equal((false, 7), h.Service.GenerateFor.Single());
        Assert.Equal("2900000000024", h.Vm.Code);
        Assert.True(h.Vm.HasSheet);
    }

    // --- Faux ---

    private sealed class FakeScanner : IBarcodeScanner
    {
        public string? Next { get; set; }
        public Task<string?> ScanAsync() => Task.FromResult(Next);
    }

    private sealed class FakeBarcodeService : ICatalogBarcodeService
    {
        public List<string> Searches { get; } = new();
        public BarcodeSearch SearchResult { get; set; } = new();
        public BarcodeMissing Missing { get; set; } = new();
        public Queue<BarcodeGenerateMissing> GenerateRuns { get; } = new();
        public int GenerateCalls { get; private set; }
        public List<(bool, int)> GenerateFor { get; } = new();
        public BarcodeGenerated Generated { get; set; } = new();
        public List<(bool, int, string?, bool)> Writes { get; } = new();
        public Queue<Exception> WriteErrors { get; } = new();
        public BarcodeWrite WriteResult { get; set; } = new();
        public List<(List<BarcodeLabelRequest>, bool)> LabelRequests { get; } = new();
        public List<string> Formats { get; } = new();

        public Task<BarcodeSearch> SearchAsync(string code) { Searches.Add(code); return Task.FromResult(SearchResult); }
        public Task<BarcodeMissing> GetMissingAsync() => Task.FromResult(Missing);

        public Task<BarcodeGenerateMissing> GenerateMissingAsync()
        {
            GenerateCalls++;
            return Task.FromResult(GenerateRuns.Count > 0 ? GenerateRuns.Dequeue() : new BarcodeGenerateMissing());
        }

        public Task<BarcodeGenerated> GenerateAsync(bool variant, int id) { GenerateFor.Add((variant, id)); return Task.FromResult(Generated); }

        public Task<BarcodeWrite> SetAsync(bool variant, int id, string? barcode, bool replace)
        {
            Writes.Add((variant, id, barcode, replace));
            if (WriteErrors.Count > 0)
                throw WriteErrors.Dequeue();
            return Task.FromResult(WriteResult);
        }

        public Task<DownloadedFile> LabelsPdfAsync(IEnumerable<BarcodeLabelRequest> labels, bool showPrice, string format = "a4-50x30")
        {
            LabelRequests.Add((labels.ToList(), showPrice));
            Formats.Add(format);
            return Task.FromResult(new DownloadedFile(new byte[] { 1 }, "application/pdf", 1));
        }
    }
}
