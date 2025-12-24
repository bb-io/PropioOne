using Blackbird.Applications.Sdk.Common;

namespace Apps.PropioOne.Webhook.Model
{
    public class ProjectWebhookSettings
    {
        [Display("Failure email")]
        public string FailureEmail { get; set; }
    }
}
