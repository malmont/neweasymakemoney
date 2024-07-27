using System;
using System.Text;

namespace Easymakemoney.Services
{
	public interface ILoginService
	{
       
        public async Task<LoginResponse> Authenticate(LoginRequest loginRequest)
        {
            string loginUrl = "https://backend-strapi.online/jeesign/api/login";
            using (var client = new HttpClient())
            {
                string loginRequestStr = JsonConvert.SerializeObject(loginRequest);
                var response = await client.PostAsync(loginUrl, new StringContent(loginRequestStr, Encoding.UTF8, "application/json"));
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<LoginResponse>(json);

                }
                else
                {
                    return null;
                }
            }
        }
    }
}

