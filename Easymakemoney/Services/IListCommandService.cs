namespace Easymakemoney.Services
{
    public interface IListCommandService
    {
        Task<ObservableCollection<ListCommand>> GetCommandesList(int id);
        Task<bool> PostCommandes(ListCommand newCommandes,int id);
        Task<bool> DeleteCommandes(int id);
    }
}