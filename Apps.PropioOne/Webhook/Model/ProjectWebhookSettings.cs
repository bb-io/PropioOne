using Blackbird.Applications.Sdk.Common;

namespace Apps.PropioOne.Webhook.Model
{
    public class ProjectWebhookSettings
    {
        [Display("Customer number")]
        public string CustomerNumber { get; set; }

        [Display("Failure email")]
        public string FailureEmail { get; set; }
    }
}
