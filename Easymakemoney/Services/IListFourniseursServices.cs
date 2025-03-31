namespace Easymakemoney.Services
{
    public interface IListFourniseursServices
    {
        Task<ObservableCollection<ListFournisseur>> GetFourniseursList();
        Task<ObservableCollection<TypeFournisseur>> GetTypeFournisseurs();
        Task<bool> PostFourniseurs(ListFournisseur newFourniseurs);
        Task<bool> DeleteFourniseurs(int id);
    }
}