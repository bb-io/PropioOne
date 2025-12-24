using Apps.PropioOne.Api;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Connections;
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

            var response = await client.ExecuteWithErrorHandling(new RestRequest("/api/v1/project/languages", Method.Get));

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("Connection validation failed.");
            }

            return new()
            {
                IsValid = true
            };
        }
        catch (Exception ex)
        {
            return new()
            {
                IsValid = false,
                Message = ex.Message
            };
        }

    }
}