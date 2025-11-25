using Apps.PropioOne.Webhook.Handler;
using Apps.PropioOne.Webhook.Model;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.Sdk.Common.Webhooks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Apps.PropioOne.Webhook
{
    [WebhookList("Project")]
    public class WebhookList(InvocationContext invocationContext) : PropioOneInvocable(invocationContext)
    {
        [Webhook("On project created", typeof(ProjectNewHandler), Description = "On new project created")]
        public Task<WebhookResponse<ProjectWebhookResponse>> ProjectCreation(
            WebhookRequest webhookRequest,
            [WebhookParameter] ProjectWebhookSettings settings)
        {
            var bodyText = webhookRequest.Body?.ToString();

            if (string.IsNullOrWhiteSpace(bodyText))
            {
                InvocationContext.Logger?.LogError(
                    "[PropioOneProjectCreation] Webhook body is empty.",
                    Array.Empty<object>());

                return Task.FromResult(new WebhookResponse<ProjectWebhookResponse>
                {
                    ReceivedWebhookRequestType = WebhookRequestType.Preflight
                });
            }

            ProjectWebhookResponse? payload;
            try
            {
                payload = JsonConvert.DeserializeObject<ProjectWebhookResponse>(bodyText);
            }
            catch (Exception ex)
            {
                InvocationContext.Logger?.LogError(
                    $"[PropioOneProjectCreation] Failed to deserialize webhook body: {ex.Message}. " +
                    $"Body: {bodyText}",
                    Array.Empty<object>());

                throw;
            }

            if (payload == null)
            {
                InvocationContext.Logger?.LogError(
                    "[PropioOneProjectCreation] Deserialized payload is null. " +
                    $"Body: {bodyText}",
                    Array.Empty<object>());

                return Task.FromResult(new WebhookResponse<ProjectWebhookResponse>
                {
                    ReceivedWebhookRequestType = WebhookRequestType.Preflight
                });
            }

            var response = new WebhookResponse<ProjectWebhookResponse>
            {
                HttpResponseMessage = null,
                Result = payload
            };

            return Task.FromResult(response);
        }
    }
}
