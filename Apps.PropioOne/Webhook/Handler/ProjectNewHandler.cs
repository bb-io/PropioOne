using Apps.PropioOne.Webhook.Model;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.Sdk.Common.Webhooks;

namespace Apps.PropioOne.Webhook.Handler
{
    public class ProjectNewHandler(InvocationContext invocationContext, [WebhookParameter(true)] ProjectWebhookSettings setting) : BaseWebhookHandler(invocationContext, SubscriptionEvent, setting)
    {
        private const string SubscriptionEvent = "ProjectNew";
    }
}
