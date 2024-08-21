namespace Easymakemoney.Services
{
    public interface IListFourniseursServices
    {
        Task<ObservableCollection<ListFournisseur>> GetFourniseursList();
        Task<bool> PostFourniseurs(ListFournisseur newFourniseurs);
        Task<bool> DeleteFourniseurs(int id);
    }
}