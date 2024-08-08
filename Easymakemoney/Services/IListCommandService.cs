namespace Easymakemoney.Services
{
    public interface IListCommandService
    {
        Task<ObservableCollection<ListCommand>> GetCommandesList(int id);
        Task<bool> PostCommandes(ListCommand newCommandes);
        Task<bool> DeleteCommandes(int id);
    }
}