using Easymakemoney.Models;
using Easymakemoney.Services;
using Easymakemoney.Services.Api;
using Easymakemoney.Tests.Fakes;
using Xunit;

namespace Easymakemoney.Tests.Services;

public class DocumentReaderServiceTests
{
    private readonly FakeHttpService _fakeHttp;
    private readonly DocumentReaderService _service;

    public DocumentReaderServiceTests()
    {
        _fakeHttp = new FakeHttpService();
        _service = new DocumentReaderService(_fakeHttp);
    }

    [Fact]
    public async Task UploadAsync_CallsPostFilesAsyncWithCorrectUrlFieldsAndFiles()
    {
        var expectedDoc = new GestionDocument { id = 42, status = "processing", type = "facture" };
        _fakeHttp.ResultToReturn = expectedDoc;

        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var files = new List<UploadFile> { new(stream, "facture.pdf", "application/pdf") };

        var result = await _service.UploadAsync("facture", files);

        Assert.NotNull(result);
        Assert.Equal(42, result.id);
        Assert.Equal("facture", result.type);
        Assert.Single(_fakeHttp.PostedFilesData);

        var (url, fields, fileField, uploadedFiles) = _fakeHttp.PostedFilesData[0];
        Assert.Equal("gestion/documents", url);
        Assert.Equal("facture", fields["type"]);
        Assert.Equal("fr", fields["locale"]);
        Assert.Equal("files[]", fileField);
        Assert.Same(files, uploadedFiles);
    }

    [Fact]
    public async Task GetAsync_CallsGetAsyncWithCorrectUrl()
    {
        var expectedDoc = new GestionDocument { id = 15, status = "done" };
        _fakeHttp.ResultToReturn = expectedDoc;

        var result = await _service.GetAsync(15);

        Assert.NotNull(result);
        Assert.Equal(15, result.id);
        Assert.Contains("gestion/documents/15", _fakeHttp.RequestedUrls);
    }

    [Fact]
    public async Task RereadAsync_CallsPostAsyncWithAuthWithCorrectUrlAndNullBody()
    {
        var expectedDoc = new GestionDocument { id = 15, status = "processing" };
        _fakeHttp.ResultToReturn = expectedDoc;

        var result = await _service.RereadAsync(15);

        Assert.NotNull(result);
        Assert.Equal(15, result.id);
        Assert.Contains(_fakeHttp.PostedAuthData, p => p.Url == "gestion/documents/15/relire" && p.Data == null);
    }

    [Fact]
    public async Task ConfirmAsync_CallsPostAsyncWithAuthWithCorrectUrlAndBody()
    {
        var payload = new { confirmedLines = new[] { 1, 2, 3 }, supplierId = 9 };

        await _service.ConfirmAsync(15, payload);

        Assert.Contains(_fakeHttp.PostedAuthData, p => p.Url == "gestion/documents/15/confirmer" && ReferenceEquals(p.Data, payload));
    }

    [Fact]
    public async Task DeleteAsync_CallsDeleAsyncWithCorrectUrl()
    {
        await _service.DeleteAsync(15);

        Assert.Contains("gestion/documents/15", _fakeHttp.DeletedUrls);
    }
}

