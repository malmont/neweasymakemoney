namespace Easymakemoney.Services
{
    public interface IListTransporteurService
    {
        Task<ObservableCollection<Transporteur>> GetListTransporteur();
       
    }
}