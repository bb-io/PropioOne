using Apps.PropioOne.Api;
using Apps.PropioOne.Constants;
using Apps.PropioOne.Webhook.Model;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.Sdk.Common.Webhooks;
using Blackbird.Applications.Sdk.Utils.Extensions.Http;
using RestSharp;
using System.Web;

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

            int customerNumber;

            var clientId = invocationContext.AuthenticationCredentialsProviders.FirstOrDefault(x => x.KeyName == CredsNames.ClientId)?.Value;

            if (string.IsNullOrWhiteSpace(clientId))
                throw new PluginMisconfigurationException("Client ID is missing in credentials.");

            if (!string.IsNullOrWhiteSpace(clientId))
            {
                if (!int.TryParse(clientId, out customerNumber))
                {
                    throw new Exception(
                        $"Customer number must be an integer, got '{clientId}'.");
                }
            }
            else
            {
                throw new Exception(
                    "Customer number is required for webhook unsubscribe.");
            }

            var getRequest =
                new RestRequest("/api/v1/project/webhooks", Method.Get);
            getRequest.AddQueryParameter("customerNumber", customerNumber);

            var webhooks =
                await client.ExecuteWithErrorHandling<List<ProjectWebhookDto>>(getRequest);

            if (webhooks == null || webhooks.Count == 0)
                return;

            var payloadUrl = values.TryGetValue(PayloadUrlKey, out var url) ? url : null;
            var eventMatches = webhooks
                .Where(w => string.Equals(w.Event, subEvent, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (eventMatches.Count == 0)
                return;

            var subscription = FindSubscription(eventMatches, payloadUrl);

            if (subscription == null)
            {
                throw new PluginApplicationException(
                    $"Failed to identify webhook subscription for event '{subEvent}'. " +
                    $"Payload URL: {payloadUrl ?? "N/A"}. Matching subscriptions found for event: {eventMatches.Count}.");
            }

            var deleteRequest =
                new RestRequest($"/api/v1/project/webhook/{subscription.Id}", Method.Delete);
            deleteRequest.AddQueryParameter("customerNumber", customerNumber);

            await client.ExecuteWithErrorHandling(deleteRequest);
        }

        private ProjectWebhookDto? FindSubscription(IEnumerable<ProjectWebhookDto> candidates, string? payloadUrl)
        {
            var candidateList = candidates.ToList();

            if (!string.IsNullOrWhiteSpace(payloadUrl))
            {
                var exactMatch = candidateList.FirstOrDefault(w =>
                    UrlsMatch(w.CallBackUrl, payloadUrl));

                if (exactMatch != null)
                    return exactMatch;
            }

            if (!string.IsNullOrWhiteSpace(setting.FailureEmail))
            {
                var emailMatches = candidateList
                    .Where(w => string.Equals(w.FailureEmail, setting.FailureEmail, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (emailMatches.Count == 1)
                    return emailMatches[0];

                if (!string.IsNullOrWhiteSpace(payloadUrl))
                {
                    var pathMatches = emailMatches
                        .Where(w => CallbackPathsMatch(w.CallBackUrl, payloadUrl))
                        .ToList();

                    if (pathMatches.Count == 1)
                        return pathMatches[0];
                }
            }

            return candidateList.Count == 1 ? candidateList[0] : null;
        }

        private static bool UrlsMatch(string? left, string? right)
        {
            var normalizedLeft = NormalizeUrl(left);
            var normalizedRight = NormalizeUrl(right);

            return !string.IsNullOrWhiteSpace(normalizedLeft) &&
                   string.Equals(normalizedLeft, normalizedRight, StringComparison.OrdinalIgnoreCase);
        }

        private static bool CallbackPathsMatch(string? left, string? right)
        {
            if (!Uri.TryCreate(left, UriKind.Absolute, out var leftUri) ||
                !Uri.TryCreate(right, UriKind.Absolute, out var rightUri))
            {
                return false;
            }

            var leftPath = HttpUtility.UrlDecode(leftUri.AbsolutePath).TrimEnd('/');
            var rightPath = HttpUtility.UrlDecode(rightUri.AbsolutePath).TrimEnd('/');

            return string.Equals(leftPath, rightPath, StringComparison.OrdinalIgnoreCase);
        }

        private static string? NormalizeUrl(string? url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return null;

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
                return url.Trim().TrimEnd('/');

            var builder = new UriBuilder(uri)
            {
                Host = uri.Host.ToLowerInvariant(),
                Path = HttpUtility.UrlDecode(uri.AbsolutePath).TrimEnd('/'),
                Fragment = string.Empty
            };

            return builder.Uri.ToString().TrimEnd('/');
        }
    }
}
