using Easymakemoney.Models;
using Easymakemoney.Services;
using Newtonsoft.Json.Linq;

namespace Easymakemoney.Tests.Fakes;

public class FakeCampaignService : ICampaignService
{
    public List<(int Page, int Limit)> ListCalls { get; } = new();
    public List<int> GetCalls { get; } = new();
    public List<JObject> CreateCalls { get; } = new();
    public List<(int Id, JObject Body)> UpdateCalls { get; } = new();
    public List<int> DeleteCalls { get; } = new();
    public List<int> GetProfitabilityCalls { get; } = new();

    public List<Campaign> ListResult { get; set; } = new();
    public Campaign CampaignResult { get; set; } = new();
    public CampaignProfitability? ProfitabilityResult { get; set; }

    public Exception? ListException { get; set; }
    public Exception? GetException { get; set; }
    public Exception? CreateException { get; set; }
    public Exception? UpdateException { get; set; }
    public Exception? DeleteException { get; set; }
    public Exception? ProfitabilityException { get; set; }

    public Task<List<Campaign>> ListAsync(int page, int limit)
    {
        ListCalls.Add((page, limit));
        if (ListException != null)
            throw ListException;
        return Task.FromResult(ListResult);
    }

    public Task<Campaign> GetAsync(int id)
    {
        GetCalls.Add(id);
        if (GetException != null)
            throw GetException;
        return Task.FromResult(CampaignResult);
    }

    public Task<Campaign> CreateAsync(JObject body)
    {
        CreateCalls.Add(body);
        if (CreateException != null)
            throw CreateException;
        return Task.FromResult(CampaignResult);
    }

    public Task<Campaign> UpdateAsync(int id, JObject body)
    {
        UpdateCalls.Add((id, body));
        if (UpdateException != null)
            throw UpdateException;
        return Task.FromResult(CampaignResult);
    }

    public Task DeleteAsync(int id)
    {
        DeleteCalls.Add(id);
        if (DeleteException != null)
            throw DeleteException;
        return Task.CompletedTask;
    }

    public Task<CampaignProfitability> GetProfitabilityAsync(int id)
    {
        GetProfitabilityCalls.Add(id);
        if (ProfitabilityException != null)
            throw ProfitabilityException;
        return Task.FromResult(ProfitabilityResult!);
    }
}

