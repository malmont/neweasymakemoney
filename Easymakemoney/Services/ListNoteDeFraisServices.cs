namespace Easymakemoney.Services
{
    public class ListNoteDeFraisServices: IListNoteDeFraisServices
    {
        private const string NoteDeFraisUrl = Configurations.BackendSymfonyUrl;
           private readonly IHttpService _httpService;
        public ListNoteDeFraisServices(IHttpService httpService)
        {
            _httpService = httpService;
        }
      
        public async Task<ObservableCollection<NoteDeFrais>> GetNoteDeFraisList(int id)
        {
            var url = $"{NoteDeFraisUrl}/collections/{id}/notes-de-frais";
            var noteDeFrais = await _httpService.GetAsync<ObservableCollection<NoteDeFrais>>(url);
            return noteDeFrais ?? new ObservableCollection<NoteDeFrais>();
        }
        public async Task<bool> PostNoteDeFrais(NoteDeFrais newNoteDeFrais, int id)
        {
            var url = $"{NoteDeFraisUrl}/collections/{id}/notes-de-frais";
            var result = await _httpService.PostAsyncWithAuth<object>(url, newNoteDeFrais);
            return result != null;
        }
        public async Task<bool> DeleteNoteDeFrais(int id)
        {
            var url = $"{NoteDeFraisUrl}/notes-de-frais/{id}";
            var result = await _httpService.DeleAsyncWithAuth<object>(url);
            return result != null;
        }
  
      
    }
}