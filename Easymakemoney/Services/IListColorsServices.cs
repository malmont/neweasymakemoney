namespace Easymakemoney.Services
{
    public interface IListColorsServices
    {
        Task<ObservableCollection<ColorsProduct>> GetColorsList();
    }
}