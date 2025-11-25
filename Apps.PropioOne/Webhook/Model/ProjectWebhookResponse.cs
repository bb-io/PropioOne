using Blackbird.Applications.Sdk.Common;
using Newtonsoft.Json;

namespace Apps.PropioOne.Webhook.Model
{
    public class ProjectWebhookResponse
    {
        [JsonProperty("event")]
        [Display("Event")]
        public string Event { get; set; } = default!;

        [JsonProperty("projectNumber")]
        [Display("Project number")]
        public string ProjectNumber { get; set; } = default!;

        [JsonProperty("sourceFileNumber")]
        [Display("Source file number")]
        public string? SourceFileNumber { get; set; }

        [JsonProperty("date")]
        [Display("Date")]
        public DateTime Date { get; set; }
    }
}
