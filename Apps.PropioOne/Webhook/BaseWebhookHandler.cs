using Apps.PropioOne.Api;
using Apps.PropioOne.Constants;
using Apps.PropioOne.Webhook.Model;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.Sdk.Common.Webhooks;
using Blackbird.Applications.Sdk.Utils.Extensions.Http;
using RestSharp;

namespace Apps.PropioOne.Webhook
{
    public class BaseWebhookHandler(InvocationContext invocationContext, string subEvent, [WebhookParameter(true)] ProjectWebhookSettings setting) : PropioOneInvocable(invocationContext), IWebhookEventHandler
    {
        private const string PayloadUrlKey = "payloadUrl";

        public async Task SubscribeAsync(IEnumerable<AuthenticationCredentialsProvider> authenticationCredentialsProvider, Dictionary<string, string> values)
        {
            if (!values.TryGetValue(PayloadUrlKey, out var payloadUrl) ||
                string.IsNullOrWhiteSpace(payloadUrl))
            {
                throw new PluginMisconfigurationException(
                    $"Missing '{PayloadUrlKey}' in webhook values.");
            }

            var clientId = invocationContext.AuthenticationCredentialsProviders.FirstOrDefault(x => x.KeyName == CredsNames.ClientId)?.Value;

            if (string.IsNullOrWhiteSpace(clientId))
                throw new PluginMisconfigurationException("Client ID is missing in credentials.");

            if (!int.TryParse(clientId, out var customerNumber))
            {
                throw new PluginMisconfigurationException(
                    $"Customer number must be an integer, got '{clientId}'.");
            }

            var request = new RestRequest("/api/v1/project/webhook/register", Method.Post);

            request.WithJsonBody(new
            {
                callBackUrl = payloadUrl,
                @event = subEvent,
                failureEmail = string.IsNullOrWhiteSpace(setting.FailureEmail)
                    ? null
                    : setting.FailureEmail,
                customerNumber = customerNumber 
            });

            await Client.ExecuteWithErrorHandling(request);
        }

        public async Task UnsubscribeAsync(IEnumerable<AuthenticationCredentialsProvider> authenticationCredentialsProvider, Dictionary<string, string> values)
        {
            try
            {
                await IdentifyAndDeleteSubscriptionAsync(authenticationCredentialsProvider, values);
            }
            catch (Exception e)
            {
                var payloadUrl = values.TryGetValue(PayloadUrlKey, out var value) ? value : "N/A";

                InvocationContext.Logger?.LogError(
                    $"[PropioOneWebhookHandler] Failed to unsubscribe from webhook ({subEvent}): {e.Message}; " +
                    $"Payload URL: {payloadUrl}",
                    Array.Empty<object>());

                throw;
            }
        }

        //helpers

        private async Task IdentifyAndDeleteSubscriptionAsync(
           IEnumerable<AuthenticationCredentialsProvider> authenticationCredentialsProvider,
           Dictionary<string, string> values)
        {
            var authProviders = authenticationCredentialsProvider as AuthenticationCredentialsProvider[]
                                 ?? authenticationCredentialsProvider.ToArray();

            var client = new PropioOneClient(authProviders);

            string? customerSegment = null;

            var clientId = invocationContext.AuthenticationCredentialsProviders.FirstOrDefault(x => x.KeyName == CredsNames.ClientId)?.Value;

            if (string.IsNullOrWhiteSpace(clientId))
                throw new PluginMisconfigurationException("Client ID is missing in credentials.");

            if (!string.IsNullOrWhiteSpace(clientId))
            {
                if (!int.TryParse(clientId, out _))
                {
                    throw new Exception(
                        $"Customer number must be an integer, got '{clientId}'.");
                }

                customerSegment = $"/{clientId}";
            }
            else
            {
                throw new Exception(
                    "Customer number is required for webhook unsubscribe.");
            }

            var getRequest =
                new RestRequest($"/api/v1/project/webhooks{customerSegment}", Method.Get);

            var webhooks =
                await client.ExecuteWithErrorHandling<List<ProjectWebhookDto>>(getRequest);

            if (webhooks == null || webhooks.Count == 0)
                return;

            var payloadUrl = values.TryGetValue(PayloadUrlKey, out var url) ? url : null;

            var subscription = webhooks.FirstOrDefault(w =>
                string.Equals(w.CallBackUrl, payloadUrl, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(w.Event, subEvent, StringComparison.OrdinalIgnoreCase));

            if (subscription == null)
                return;

            var deleteRequest =
                new RestRequest($"/api/v1/project/webhooks/{subscription.Id}", Method.Delete);

            await client.ExecuteWithErrorHandling(deleteRequest);
        }
    }
}
