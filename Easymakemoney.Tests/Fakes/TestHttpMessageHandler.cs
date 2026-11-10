using System.Net;

namespace Easymakemoney.Tests.Fakes;

public class TestHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _handlerFunc;
    public List<HttpRequestMessage> SentRequests { get; } = new();

    public TestHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
    {
        _handlerFunc = req => Task.FromResult(handler(req));
    }

    public TestHttpMessageHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handlerFunc)
    {
        _handlerFunc = handlerFunc;
    }

    public TestHttpMessageHandler(HttpResponseMessage staticResponse)
    {
        _handlerFunc = _ => Task.FromResult(staticResponse);
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        SentRequests.Add(request);
        return await _handlerFunc(request);
    }
}

