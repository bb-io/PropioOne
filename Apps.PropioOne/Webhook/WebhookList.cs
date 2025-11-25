using Apps.PropioOne.Webhook.Handler;
using Apps.PropioOne.Webhook.Model;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.Sdk.Common.Webhooks;
using Newtonsoft.Json.Linq;

namespace Apps.PropioOne.Webhook
{
    [WebhookList]
    public class WebhookList(InvocationContext invocationContext) : PropioOneInvocable(invocationContext)
    {
        [Webhook("On project created", typeof(ProjectNewHandler), Description = "On new project created")]
        public Task<WebhookResponse<JObject>> ProjectCreation(
            WebhookRequest webhookRequest,
            [WebhookParameter] ProjectWebhookSettings settings)
        {
            var bodyText = webhookRequest.Body?.ToString();

            if (string.IsNullOrWhiteSpace(bodyText))
            {
                InvocationContext.Logger?.LogError("[PropioOneProjectCreation] Webhook body is empty.",
                    Array.Empty<object>());

                throw new InvalidOperationException("Webhook body is empty.");
            }

            JObject payload;
            try
            {
                payload = JObject.Parse(bodyText);
            }
            catch (Exception ex)
            {
                InvocationContext.Logger?.LogError(
                    $"[PropioOneProjectCreation] Failed to parse webhook body: {ex.Message}. " +
                    $"Body: {bodyText}",
                    Array.Empty<object>());

                throw;
            }

            var response = new WebhookResponse<JObject>
            {
                HttpResponseMessage = null,
                Result = payload
            };

            return Task.FromResult(response);
        }
    }
}
