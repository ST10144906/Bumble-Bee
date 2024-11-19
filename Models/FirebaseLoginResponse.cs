using Newtonsoft.Json;

namespace BumbleBeeWebApp.Models
{
    public class FirebaseLoginResponse
    {
        [JsonProperty("localId")]
        public string LocalId { get; set; }

        [JsonProperty("idToken")]
        public string IdToken { get; set; }

        [JsonProperty("refreshToken")]
        public string RefreshToken { get; set; }

        [JsonProperty("expiresIn")]
        public string ExpiresIn { get; set; }
    }
}
