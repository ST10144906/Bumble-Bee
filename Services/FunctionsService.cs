using System.Net.Http;
using System.Threading.Tasks;


    //--- FUNCTIONS ARE NOT SETUP IN FIREBASE PROJECT CURRENTLY
    public class FunctionsService
    {
        private readonly HttpClient _httpClient;

        public FunctionsService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<string> CallFunctionAsync(string functionName, object data)
        {
            var response = await _httpClient.PostAsJsonAsync(
                $"https://us-central1-bumble-bee-foundation.cloudfunctions.net/{functionName}", data); //--- just for testing purposes, can change this ot be the actual domain for the website later on.
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        //--- Additional methods for  Firebase Functions operations to be added later.
    }

