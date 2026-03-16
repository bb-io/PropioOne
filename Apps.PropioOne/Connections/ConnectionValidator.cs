using Apps.PropioOne.Api;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Connections;
using Blackbird.Applications.Sdk.Common.Exceptions;
using RestSharp;

namespace Apps.PropioOne.Connections;

public class ConnectionValidator : IConnectionValidator
{
    public async ValueTask<ConnectionValidationResponse> ValidateConnection(
        IEnumerable<AuthenticationCredentialsProvider> authenticationCredentialsProviders,
        CancellationToken cancellationToken)
    {
        try
        {
            var client = new PropioOneClient(authenticationCredentialsProviders);

            await client.ExecuteWithErrorHandling(
                new RestRequest("/api/v1/project/languages", Method.Get));


        }
        catch (PluginApplicationException ex) when (
            ex.Message.Contains("status 400") ||
            ex.Message.Contains("status 401") ||
            ex.Message.Contains("status 403"))
        {
            return new()
            {
                IsValid = false,
                Message = ex.Message
            };
        }
        catch
        {
            return new()
            {
                IsValid = true
            };
        }

        return new()
        {
            IsValid = true
        };
    }
}