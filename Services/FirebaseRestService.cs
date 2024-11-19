using BumbleBeeWebApp.Models;
using Newtonsoft.Json;

namespace BumbleBeeWebApp.Services
{
    public class FirebaseRestService
    {
        private readonly string _apiKey;

        public FirebaseRestService(string apiKey)
        {
            _apiKey = apiKey;
        }

        public async Task<FirebaseLoginResponse> LoginUserAsync(String email, String password) 
        {
            using (var client = new HttpClient()) 
            {
                var url = $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key=AIzaSyCGItelKihS1fQC0C7Tj8v-5s0KoRc_IuM";

                var payload = new
                {
                    email = email,
                    password = password,
                    secureToken = true
                };

                var jsonPayload = JsonConvert.SerializeObject(payload);

                var response = await client.PostAsync(url, new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json"));

                var jsonResponse = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode) 
                {
                    return JsonConvert.DeserializeObject<FirebaseLoginResponse>(jsonResponse);
                }
                else 
                {
                    var errorResponse = JsonConvert.DeserializeObject<FirebaseErrorResponse>(jsonResponse);
                    throw new Exception($"Login failed: {errorResponse.Error.Message}");
                }
            }
        }
    }
}
