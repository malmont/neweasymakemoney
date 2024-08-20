namespace Easymakemoney.Services
{
    public interface IListFourniseursServices
    {
        Task<ObservableCollection<ListFournisseur>> GetFourniseursList(int id);
        Task<bool> PostFourniseurs(ListFournisseur newFourniseurs,int id);
        Task<bool> DeleteFourniseurs(int id);
    }
}