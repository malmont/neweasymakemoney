using Easymakemoney.Models;
using Easymakemoney.Services;

namespace Easymakemoney.Tests.Fakes;

public class FakeTripService : ITripService
{
    public List<(int Page, int Limit)> GetTripsCalls { get; } = new();
    public List<int> GetTripCalls { get; } = new();
    public List<int> GetFollowUpCalls { get; } = new();
    public List<TripRequest> CreateCalls { get; } = new();
    public List<(int TripId, TripRequest Request)> UpdateCalls { get; } = new();
    public List<int> DeleteCalls { get; } = new();
    public List<(int? TripId, List<int> NoteIds)> AttachNotesCalls { get; } = new();
    public List<(int? TripId, List<int> CommandeIds)> AttachCommandesCalls { get; } = new();

    public List<Trip> TripsResult { get; set; } = new();
    public Trip TripResult { get; set; } = new();
    public TripFollowUp FollowUpResult { get; set; } = new();

    public Exception? GetTripsException { get; set; }
    public Exception? GetTripException { get; set; }
    public Exception? GetFollowUpException { get; set; }
    public Exception? CreateException { get; set; }
    public Exception? UpdateException { get; set; }
    public Exception? DeleteException { get; set; }

    public Task<List<Trip>> GetTripsAsync(int page = 1, int limit = 30)
    {
        GetTripsCalls.Add((page, limit));
        if (GetTripsException != null)
            throw GetTripsException;
        return Task.FromResult(TripsResult);
    }

    public Task<Trip> GetTripAsync(int tripId)
    {
        GetTripCalls.Add(tripId);
        if (GetTripException != null)
            throw GetTripException;
        return Task.FromResult(TripResult);
    }

    public Task<TripFollowUp> GetFollowUpAsync(int tripId)
    {
        GetFollowUpCalls.Add(tripId);
        if (GetFollowUpException != null)
            throw GetFollowUpException;
        return Task.FromResult(FollowUpResult);
    }

    public Task<Trip> CreateAsync(TripRequest request)
    {
        CreateCalls.Add(request);
        if (CreateException != null)
            throw CreateException;
        return Task.FromResult(TripResult);
    }

    public Task<Trip> UpdateAsync(int tripId, TripRequest request)
    {
        UpdateCalls.Add((tripId, request));
        if (UpdateException != null)
            throw UpdateException;
        return Task.FromResult(TripResult);
    }

    public Task DeleteAsync(int tripId)
    {
        DeleteCalls.Add(tripId);
        if (DeleteException != null)
            throw DeleteException;
        return Task.CompletedTask;
    }

    public Task AttachNotesAsync(int? tripId, IEnumerable<int> noteIds)
    {
        AttachNotesCalls.Add((tripId, noteIds.ToList()));
        return Task.CompletedTask;
    }

    public Task AttachCommandesAsync(int? tripId, IEnumerable<int> commandeIds)
    {
        AttachCommandesCalls.Add((tripId, commandeIds.ToList()));
        return Task.CompletedTask;
    }
}

