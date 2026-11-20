namespace Easymakemoney.Services
{
    /// <summary>Prêts (lot backend FIS-1) : routes gestion/prets. Montants en cents, taux en millionièmes, dates AAAA-MM-JJ.</summary>
    public interface ILoanService
    {
        /// <summary>Les prêts, du plus récent au plus ancien, avec le résumé du jour.</summary>
        Task<LoanList> ListAsync();

        /// <summary>La fiche, son résumé et l'échéancier complet.</summary>
        Task<Loan> GetAsync(int id);

        /// <summary>Aperçu calculé par le serveur, sans rien écrire ; la demande est annulée avec le jeton.</summary>
        Task<LoanPreview> PreviewAsync(IReadOnlyDictionary<string, object?> body, CancellationToken cancellationToken = default);

        Task<Loan?> CreateAsync(IReadOnlyDictionary<string, object?> body);

        /// <summary>PATCH : seuls les champs envoyés changent, les absents sont gardés.</summary>
        Task<Loan?> UpdateAsync(int id, IReadOnlyDictionary<string, object?> changes);

        Task DeleteAsync(int id);

        Task AddPrepaymentAsync(int id, DateTime date, long amountCents, long? feesCents);

        /// <summary>Retire un remboursement anticipé (index = rang dans l'ordre des dates) ; rend la fiche recalculée.</summary>
        Task<Loan> DeletePrepaymentAsync(int id, int index);
    }
}
