
namespace Easymakemoney.Services
{
    public class ListCollectionService : IListCollectionService
    {
        public async Task<ObservableCollection<ListCollection>> GetCollectionList()
        {
            string getListUrl = "https://backend-strapi.online/jeesign/api/collections.json";
            using (var client = new HttpClient())
            {
                var response = await client.GetAsync(getListUrl);
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var deserializeJson = JsonConvert.DeserializeObject<ObservableCollection<ListCollection>>(json);
                    return deserializeJson;
                }
                else
                {
                    return new ObservableCollection<ListCollection>(); // Retourne une collection vide en cas d'erreur
                }
            }
        }
    }
}
