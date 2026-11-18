using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Easymakemoney.Services.Session;

namespace Easymakemoney.Services.Incidents;

public enum SendOutcome
{
    /// <summary>202 : accepté, le rapport sort de la file.</summary>
    Accepted,
    /// <summary>422 (ou 401 sans jeton) : rapport refusé, jeté sans nouvel essai.</summary>
    Dropped,
    /// <summary>429 : attendre RetryAfter avant le prochain envoi.</summary>
    RetryLater,
    /// <summary>Autre réponse ou panne réseau : le rapport reste dans la file.</summary>
    Keep,
}

public interface IIncidentSender
{
    Task<(SendOutcome Outcome, TimeSpan? RetryAfter)> SendAsync(string body);
}

/// <summary>
/// Envoi par le client nommé « auth » (site seulement, sans rafraîchissement de session) : un échec d'envoi ne crée jamais
/// d'incident ni de boucle. Jeton de l'utilisateur s'il est connecté ; sur un 401, un seul renvoi sans jeton.
/// </summary>
public class HttpIncidentSender : IIncidentSender
{
    private const string Route = "incidents/report";
    private readonly IHttpClientFactory _factory;
    private readonly ISessionService _session;

    public HttpIncidentSender(IHttpClientFactory factory, ISessionService session)
    {
        _factory = factory;
        _session = session;
    }

    public async Task<(SendOutcome Outcome, TimeSpan? RetryAfter)> SendAsync(string body)
    {
        try
        {
            string? token = null;
            try
            {
                token = await _session.GetTokenAsync();
            }
            catch (Exception)
            {
                token = null;
            }

            var response = await PostAsync(body, token);
            if (response.StatusCode == HttpStatusCode.Unauthorized && token != null)
            {
                response.Dispose();
                response = await PostAsync(body, null);
            }

            using (response)
            {
                return response.StatusCode switch
                {
                    HttpStatusCode.Accepted or HttpStatusCode.OK or HttpStatusCode.Created => (SendOutcome.Accepted, null),
                    HttpStatusCode.UnprocessableEntity or HttpStatusCode.Unauthorized or HttpStatusCode.BadRequest => (SendOutcome.Dropped, null),
                    HttpStatusCode.TooManyRequests => (SendOutcome.RetryLater, response.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(60)),
                    _ => (SendOutcome.Keep, null),
                };
            }
        }
        catch (Exception)
        {
            // Site inconnu, réseau absent : le rapport attend dans la file.
            return (SendOutcome.Keep, null);
        }
    }

    private async Task<HttpResponseMessage> PostAsync(string body, string? token)
    {
        var client = _factory.CreateClient("auth");
        var request = new HttpRequestMessage(HttpMethod.Post, Route)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };
        if (!string.IsNullOrWhiteSpace(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await client.SendAsync(request);
    }
}
