namespace Easymakemoney.Models
{
    public interface IListNoteDeFraisServices
    {
        Task<ObservableCollection<NoteDeFrais>> GetNoteDeFraisList(int id);
        Task<bool> PostNoteDeFrais(NoteDeFrais newNoteDeFrais, int id);
        Task<bool> DeleteNoteDeFrais(int id);
        Task<ObservableCollection<TypeNoteDeFrais>> GetTypeNoteDeFrais();
      
    }
}