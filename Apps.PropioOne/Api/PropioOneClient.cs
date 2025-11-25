using Apps.PropioOne.Constants;
using Apps.PropioOne.Models;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Utils.Extensions.Sdk;
using Blackbird.Applications.Sdk.Utils.RestSharp;
using Newtonsoft.Json;
using RestSharp;

namespace Apps.PropioOne.Api;

public class PropioOneClient : BlackBirdRestClient
{
    public PropioOneClient(IEnumerable<AuthenticationCredentialsProvider> creds) : base(new()
    {
        BaseUrl = new Uri(creds.Get(CredsNames.Url).Value),
    })
    {
        var token =  GetToken(creds).GetAwaiter().GetResult();
        this.AddDefaultHeader("Authorization", $"Bearer {token}");
    }

    protected override Exception ConfigureErrorException(RestResponse response)
    {
        var error = JsonConvert.DeserializeObject(response.Content);
        throw new PluginApplicationException($"{error}");
    }

    public override async Task<T> ExecuteWithErrorHandling<T>(RestRequest request)
    {
        string content = (await ExecuteWithErrorHandling(request)).Content;
        T val = JsonConvert.DeserializeObject<T>(content, JsonSettings);
        if (val == null)
        {
            throw new Exception($"Could not parse {content} to {typeof(T)}");
        }

        return val;
    }

    public override async Task<RestResponse> ExecuteWithErrorHandling(RestRequest request)
    {
        RestResponse restResponse = await ExecuteAsync(request);
        if (!restResponse.IsSuccessStatusCode)
        {
            throw ConfigureErrorException(restResponse);
        }

        return restResponse;
    }

    private async Task<string> GetToken(IEnumerable<AuthenticationCredentialsProvider> creds)
    {
        var clientId = creds.Get(CredsNames.ClientId).Value;
        var clientSecret = creds.Get(CredsNames.ClientSecret).Value;
        var apiUrl = creds.Get(CredsNames.Url).Value;

        if (string.IsNullOrWhiteSpace(apiUrl))
            throw new PluginMisconfigurationException("Base API URL is missing in credentials.");

        string idsBaseUrl;
        if (apiUrl.Contains("dev", StringComparison.OrdinalIgnoreCase))
        {
            idsBaseUrl = "https://ulg-identity-server-api-dev.azurewebsites.net";
        }
        else
        {
            idsBaseUrl = "https://ulg-identity-server-api-prod.azurewebsites.net";
        }

        var request = new RestRequest($"{idsBaseUrl}/connect/token", Method.Post);
        request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
        request.AddParameter("grant_type", "client_credentials");
        request.AddParameter("client_id", clientId);
        request.AddParameter("client_secret", clientSecret);

        var tokenClient = new RestClient();
        var response = await ExecuteWithErrorHandling<AccessTokenResponse>(request);

        if (string.IsNullOrEmpty(response.AccessToken))
        {
            throw new PluginApplicationException($"Failed to retrieve access token.");
        }

        return response.AccessToken;
    }

}