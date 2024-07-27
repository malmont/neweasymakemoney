using System;
using System.Text;

namespace Easymakemoney.Services
{
	public interface IListCollectionService
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
                    var desiarizeJson = JsonConvert.DeserializeObject<ObservableCollection<ListCollection>>(json);
                    return desiarizeJson;

                }
                else
                {
                    return null;
                }
            }

            }
    }
}

