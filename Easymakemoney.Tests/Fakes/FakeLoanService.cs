using Easymakemoney.Models;
using Easymakemoney.Services;

namespace Easymakemoney.Tests.Fakes;

/// <summary>Faux service des prêts : chaque aperçu attend une réponse donnée par le test, dans l'ordre voulu.</summary>
public class FakeLoanService : ILoanService
{
    public LoanList List { get; set; } = new();
    public Loan Loan { get; set; } = new() { id = 1, label = "Prêt" };
    public Exception? WriteError { get; set; }

    public List<(IReadOnlyDictionary<string, object?> Body, CancellationToken Token, TaskCompletionSource<LoanPreview> Answer)> Previews { get; } = new();
    public List<IReadOnlyDictionary<string, object?>> Created { get; } = new();
    public List<(int Id, IReadOnlyDictionary<string, object?> Changes)> Updated { get; } = new();
    public List<int> Deleted { get; } = new();
    public List<(int Id, DateTime Date, long Amount, long? Fees)> AddedPrepayments { get; } = new();
    public List<(int Id, int Index)> DeletedPrepayments { get; } = new();
    public List<int> Fetched { get; } = new();

    public Task<LoanList> ListAsync() => Task.FromResult(List);

    public Task<Loan> GetAsync(int id)
    {
        Fetched.Add(id);
        return Task.FromResult(Loan);
    }

    /// <summary>La réponse vient quand le test appelle Answer ; le jeton n'interrompt pas la réponse (serveur lent).</summary>
    public Task<LoanPreview> PreviewAsync(IReadOnlyDictionary<string, object?> body, CancellationToken cancellationToken = default)
    {
        var answer = new TaskCompletionSource<LoanPreview>(TaskCreationOptions.RunContinuationsAsynchronously);
        Previews.Add((body, cancellationToken, answer));
        return answer.Task;
    }

    public Task<Loan?> CreateAsync(IReadOnlyDictionary<string, object?> body)
    {
        if (WriteError != null)
            return Task.FromException<Loan?>(WriteError);
        Created.Add(body);
        return Task.FromResult<Loan?>(Loan);
    }

    public Task<Loan?> UpdateAsync(int id, IReadOnlyDictionary<string, object?> changes)
    {
        if (WriteError != null)
            return Task.FromException<Loan?>(WriteError);
        Updated.Add((id, changes));
        return Task.FromResult<Loan?>(Loan);
    }

    public Task DeleteAsync(int id)
    {
        if (WriteError != null)
            return Task.FromException(WriteError);
        Deleted.Add(id);
        return Task.CompletedTask;
    }

    public Task AddPrepaymentAsync(int id, DateTime date, long amountCents, long? feesCents)
    {
        if (WriteError != null)
            return Task.FromException(WriteError);
        AddedPrepayments.Add((id, date, amountCents, feesCents));
        return Task.CompletedTask;
    }

    public Task<Loan> DeletePrepaymentAsync(int id, int index)
    {
        if (WriteError != null)
            return Task.FromException<Loan>(WriteError);
        DeletedPrepayments.Add((id, index));
        return Task.FromResult(Loan);
    }
}
