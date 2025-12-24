using Apps.PropioOne.Constants;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Connections;

namespace Apps.PropioOne.Connections;

public class ConnectionDefinition : IConnectionDefinition
{
    public IEnumerable<ConnectionPropertyGroup> ConnectionPropertyGroups => new List<ConnectionPropertyGroup>
    {
        new()
        {
            Name = "Developer API key",
            AuthenticationType = ConnectionAuthenticationType.Undefined,
            ConnectionProperties = new List<ConnectionProperty>
            {
                new(CredsNames.ClientAppId) { DisplayName = "Client app ID"},
                new(CredsNames.ClientSecret) { DisplayName = "Client secret"},
                new(CredsNames.Url) { DisplayName = "Base URL",
                Description="Select the base URL",
                DataItems=
                [
                    new ("https://tgw-dev.propio-ls.com","Develop enviroment"),
                    new ("https://tgw.propio-ls.com","Production enviroment")
                 ]},
                new(CredsNames.ClientId) { DisplayName = "Client number"},
            }
        }
    };

    public IEnumerable<AuthenticationCredentialsProvider> CreateAuthorizationCredentialsProviders(
        Dictionary<string, string> values) => values.Select(x => new AuthenticationCredentialsProvider(x.Key, x.Value)
        ).ToList();
}