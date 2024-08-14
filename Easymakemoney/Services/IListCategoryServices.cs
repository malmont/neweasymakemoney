namespace Easymakemoney.Services
{
    public interface IListCategoryServices
    {
        Task<ObservableCollection<Category>> GetCategoryList();
      
    }
}
