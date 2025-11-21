using Newtonsoft.Json;

namespace Apps.PropioOne.Models
{
    public class AccessTokenResponse
    {
        [JsonProperty("access_token")]
        public string AccessToken { get; set; }
    }
}
