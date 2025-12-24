using Blackbird.Applications.Sdk.Common;
using Newtonsoft.Json;

namespace Apps.PropioOne.Webhook.Model
{
    public class ProjectWebhookDto
    {
        [JsonProperty("id")]
        [Display("Id")]
        public long Id { get; set; }

        [JsonProperty("callBackUrl")]
        [Display("Callback URL")]
        public string? CallBackUrl { get; set; }

        [JsonProperty("event")]
        [Display("Event")]
        public string? Event { get; set; }

        [JsonProperty("failureEmail")]
        [Display("Failure email")]
        public string? FailureEmail { get; set; }
    }
}
