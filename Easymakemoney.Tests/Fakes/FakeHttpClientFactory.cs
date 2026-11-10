namespace Easymakemoney.Tests.Fakes;

public class FakeHttpClientFactory : IHttpClientFactory
{
    private readonly Func<string, HttpClient> _factory;

    public FakeHttpClientFactory(HttpClient client)
    {
        _factory = _ => client;
    }

    public FakeHttpClientFactory(Func<string, HttpClient> factory)
    {
        _factory = factory;
    }

    public HttpClient CreateClient(string name)
    {
        return _factory(name);
    }
}

