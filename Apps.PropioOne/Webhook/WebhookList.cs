using Apps.PropioOne.Webhook.Handler;
using Apps.PropioOne.Webhook.Model;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.Sdk.Common.Webhooks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Apps.PropioOne.Webhook
{
    [WebhookList("Order")]
    public class WebhookList(InvocationContext invocationContext) : PropioOneInvocable(invocationContext)
    {
        [Webhook("On order created", typeof(ProjectNewHandler), Description = "On new order created")]
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

        [Webhook("On order in progress", typeof(ProjectInProgressHandler), Description = "On order in progress")]
        public Task<WebhookResponse<ProjectWebhookResponse>> ProjectInProgress(
            WebhookRequest webhookRequest,
            [WebhookParameter] ProjectWebhookSettings settings)
            => ProjectCreation(webhookRequest, settings);

        [Webhook("On order completed", typeof(ProjectCompletedHandler), Description = "On order completed")]
        public Task<WebhookResponse<ProjectWebhookResponse>> ProjectCompleted(
            WebhookRequest webhookRequest,
            [WebhookParameter] ProjectWebhookSettings settings)
            => ProjectCreation(webhookRequest, settings);

        [Webhook("On order canceled", typeof(ProjectCanceledHandler), Description = "On order canceled")]
        public Task<WebhookResponse<ProjectWebhookResponse>> ProjectCanceled(
            WebhookRequest webhookRequest,
            [WebhookParameter] ProjectWebhookSettings settings)
            => ProjectCreation(webhookRequest, settings);

        [Webhook("On job completed", typeof(JobCompletedHandler), Description = "On job completed")]
        public Task<WebhookResponse<ProjectWebhookResponse>> JobCompleted(
            WebhookRequest webhookRequest,
            [WebhookParameter] ProjectWebhookSettings settings)
            => ProjectCreation(webhookRequest, settings);

        [Webhook("On automation job completed", typeof(AutomationJobCompletedHandler), Description = "On automation job completed")]
        public Task<WebhookResponse<ProjectWebhookResponse>> AutomationJobCompleted(
            WebhookRequest webhookRequest,
            [WebhookParameter] ProjectWebhookSettings settings)
            => ProjectCreation(webhookRequest, settings);

        [Webhook("On translation complete", typeof(TranslationCompleteHandler), Description = "On translation complete")]
        public Task<WebhookResponse<ProjectWebhookResponse>> TranslationComplete(
            WebhookRequest webhookRequest,
            [WebhookParameter] ProjectWebhookSettings settings)
            => ProjectCreation(webhookRequest, settings);
    }
}
