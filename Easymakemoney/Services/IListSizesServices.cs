namespace Easymakemoney.Services
{
    public interface IListSizesServices
    {
        Task<ObservableCollection<SizeProduct>> GetSizesList();
    }
}