using System.Collections.ObjectModel;
using System.Globalization;
using System.Net;
using System.Text;
using Easymakemoney.Converters;
using Easymakemoney.Models;
using Easymakemoney.Services;
using Easymakemoney.Services.Api;
using Easymakemoney.Tests.Fakes;
using Easymakemoney.UseCase.ListFraisDePortUseCase;
using Easymakemoney.UseCase.ListNoteDeFraisUseCase;
using Easymakemoney.UseCase.ListTransporteurUseCase;
using Easymakemoney.ViewModels.FormModels;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Easymakemoney.Tests.Services;

public class FraisEtNotesServicesTests : IDisposable
{
    private readonly CultureInfo _originalCulture;

    public FraisEtNotesServicesTests()
    {
        _originalCulture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("fr-CA");
    }

    public void Dispose()
    {
        CultureInfo.CurrentCulture = _originalCulture;
    }

    private class FakeTransporteurService : IListTransporteurService
    {
        public Task<ObservableCollection<Transporteur>> GetListTransporteur() =>
            Task.FromResult(new ObservableCollection<Transporteur>());
    }

    private class FakeTypeNoteService : IListNoteDeFraisServices
    {
        public Task<ObservableCollection<NoteDeFrais>> GetNoteDeFraisList(int id) =>
            Task.FromResult(new ObservableCollection<NoteDeFrais>());

        public Task<bool> PostNoteDeFrais(NoteDeFrais newNoteDeFrais, int id) => Task.FromResult(true);

        public Task<ObservableCollection<TypeNoteDeFrais>> GetTypeNoteDeFrais() =>
            Task.FromResult(new ObservableCollection<TypeNoteDeFrais>());

        public Task<bool> DeleteNoteDeFrais(int id) => Task.FromResult(true);
    }

    [Theory]
    [InlineData("12,50", 1250L)]
    [InlineData("1 234,50", 123450L)]
    [InlineData("1\u00A0234,50", 123450L)]
    [InlineData("1\u202F234,50", 123450L)]
    public async Task FraisDePort_FullChainFromTextEntryToSend_SendsCentsInPrice(string rawInput, long expectedCents)
    {
        HttpRequestMessage? capturedRequest = null;
        string? capturedBody = null;

        var handler = new TestHttpMessageHandler(async req =>
        {
            capturedRequest = req;
            if (req.Content != null)
                capturedBody = await req.Content.ReadAsStringAsync();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            };
        });

        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/api/") };
        var httpService = new HttpService(client);
        var fraisService = new ListFraisDePortService(httpService);
        var createUseCase = new CreateFraisDePortUseCase(fraisService);
        var saveUseCase = new SaveFraisDePortUseCase(createUseCase);
        var transporteurUseCase = new GetListTransporteurUseCase(new FakeTransporteurService());

        var form = new FraisDePortFormModel(transporteurUseCase)
        {
            Name = "Livraison",
            Prix = DecimalInputConverter.Parse(rawInput),
            Facture = "F-01",
            Tracknumber = "TRK-01",
            Image = "img.png"
        };

        var result = await saveUseCase.ExecuteAsync(form, 8);

        Assert.True(result);
        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Post, capturedRequest.Method);
        Assert.Equal("http://localhost/api/commandes/8/frais-de-port", capturedRequest.RequestUri?.ToString());

        Assert.NotNull(capturedBody);
        var json = JObject.Parse(capturedBody);
        Assert.Equal(expectedCents, (long?)json["price"]);
    }

    [Fact]
    public async Task FraisDePort_DeleteFraisDePort_CallsCorrectDeleteUrl()
    {
        HttpRequestMessage? capturedRequest = null;
        var handler = new TestHttpMessageHandler(req =>
        {
            capturedRequest = req;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            };
        });

        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/api/") };
        var httpService = new HttpService(client);
        var fraisService = new ListFraisDePortService(httpService);

        var result = await fraisService.DeleteFraisDePort(8);

        Assert.True(result);
        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Delete, capturedRequest.Method);
        Assert.Equal("http://localhost/api/commandes/8/frais-de-port", capturedRequest.RequestUri?.ToString());
    }

    [Fact]
    public async Task FraisDePort_PeriodClosed_ThrowsApiExceptionFromService()
    {
        var errorJson = """{ "error": "PERIOD_CLOSED", "message": "Cette période est clôturée" }""";
        var handler = new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.Conflict)
        {
            Content = new StringContent(errorJson, Encoding.UTF8, "application/json")
        });

        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/api/") };
        var httpService = new HttpService(client);
        var fraisService = new ListFraisDePortService(httpService);

        var frais = new FraisDePort { name = "Colissimo", image = "x.png", price = 1250 };
        var ex = await Assert.ThrowsAsync<ApiException>(() => fraisService.PostFraisDePort(frais, 8));

        Assert.Equal(HttpStatusCode.Conflict, ex.StatusCode);
        Assert.Equal("PERIOD_CLOSED", ex.ApiError.Code);
    }

    [Theory]
    [InlineData("12,50", 1250L)]
    [InlineData("1 234,50", 123450L)]
    [InlineData("1\u00A0234,50", 123450L)]
    [InlineData("1\u202F234,50", 123450L)]
    public async Task NoteDeFrais_FullChainFromTextEntryToSend_SendsCentsInMontant(string rawInput, long expectedCents)
    {
        HttpRequestMessage? capturedRequest = null;
        string? capturedBody = null;

        var handler = new TestHttpMessageHandler(async req =>
        {
            capturedRequest = req;
            if (req.Content != null)
                capturedBody = await req.Content.ReadAsStringAsync();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            };
        });

        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/api/") };
        var httpService = new HttpService(client);
        var noteService = new ListNoteDeFraisServices(httpService);
        var createUseCase = new CreateNoteDeFraisUseCase(noteService);
        var saveUseCase = new SaveNoteDeFraisUseCase(createUseCase);
        var typeUseCase = new GestListTypeNoteDeFraisUseCase(new FakeTypeNoteService());

        var form = new NoteDeFraisForModel(typeUseCase)
        {
            Name = "Repas",
            Montant = DecimalInputConverter.Parse(rawInput),
            Date = new DateTime(2026, 5, 10),
            Description = "Déjeuner",
            SelectedTypeNoteDeFrais = new TypeNoteDeFrais { id = 4, name = "Repas" }
        };

        var result = await saveUseCase.ExecuteAsync(form, 3);

        Assert.True(result);
        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Post, capturedRequest.Method);
        Assert.Equal("http://localhost/api/collections/3/notes-de-frais", capturedRequest.RequestUri?.ToString());

        Assert.NotNull(capturedBody);
        var json = JObject.Parse(capturedBody);
        Assert.Equal(expectedCents, (long?)json["montant"]);
    }

    [Fact]
    public async Task NoteDeFrais_DeleteNoteDeFrais_CallsCorrectDeleteUrl()
    {
        HttpRequestMessage? capturedRequest = null;
        var handler = new TestHttpMessageHandler(req =>
        {
            capturedRequest = req;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            };
        });

        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/api/") };
        var httpService = new HttpService(client);
        var noteService = new ListNoteDeFraisServices(httpService);

        var result = await noteService.DeleteNoteDeFrais(19);

        Assert.True(result);
        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Delete, capturedRequest.Method);
        Assert.Equal("http://localhost/api/notes-de-frais/19", capturedRequest.RequestUri?.ToString());
    }

    [Fact]
    public async Task NoteDeFrais_PeriodClosed_ThrowsApiExceptionFromService()
    {
        var errorJson = """{ "error": "PERIOD_CLOSED", "message": "Cette période est clôturée" }""";
        var handler = new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.Conflict)
        {
            Content = new StringContent(errorJson, Encoding.UTF8, "application/json")
        });

        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/api/") };
        var httpService = new HttpService(client);
        var noteService = new ListNoteDeFraisServices(httpService);

        var note = new NoteDeFrais { name = "Fournitures", montant = 1250 };
        var ex = await Assert.ThrowsAsync<ApiException>(() => noteService.PostNoteDeFrais(note, 3));

        Assert.Equal(HttpStatusCode.Conflict, ex.StatusCode);
        Assert.Equal("PERIOD_CLOSED", ex.ApiError.Code);
    }
}

