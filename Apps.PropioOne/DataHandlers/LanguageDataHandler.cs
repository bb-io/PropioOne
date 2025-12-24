using Apps.PropioOne.Models;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;
using RestSharp;

namespace Apps.PropioOne.Handlers;
public class LanguageDataHandler(InvocationContext invocationContext) : PropioOneInvocable(invocationContext), IAsyncDataSourceItemHandler
{
    public async Task<IEnumerable<DataSourceItem>> GetDataAsync(
        DataSourceContext context,
        CancellationToken cancellationToken)
    {
        var request = new RestRequest("/api/v1/project/languages", Method.Get);

        var languages = await Client.ExecuteWithErrorHandling<List<LanguageDto>>(request);

        if (!string.IsNullOrWhiteSpace(context.SearchString))
        {
            var search = context.SearchString.Trim();
            languages = languages
                .Where(l =>
                    l.Code.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    l.Name.Contains(search, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        return languages
            .OrderBy(l => l.Name)
            .Select(l => new DataSourceItem(
                value: l.Code,
                displayName: $"{l.Name} ({l.Code})"));
    }
}
