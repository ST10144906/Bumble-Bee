using Newtonsoft.Json;

namespace BumbleBeeWebApp.Models
{
    public class FirebaseErrorResponse
    {
        [JsonProperty("error")]
        public FirebaseError Error { get; set; }
    }

    public class FirebaseError
    {
        [JsonProperty("message")]
        public string Message { get; set; }
    }
}
