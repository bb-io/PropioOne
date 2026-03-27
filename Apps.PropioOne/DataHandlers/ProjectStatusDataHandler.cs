using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;
using RestSharp;

namespace Apps.PropioOne.Handlers;

public class ProjectStatusDataHandler(InvocationContext invocationContext)
    : PropioOneInvocable(invocationContext), IAsyncDataSourceItemHandler
{
    public async Task<IEnumerable<DataSourceItem>> GetDataAsync(
        DataSourceContext context,
        CancellationToken cancellationToken)
    {
        var request = new RestRequest("/api/v1/project/Statuses", Method.Get);

        var statuses = await Client.ExecuteWithErrorHandling<List<string>>(request);

        if (!string.IsNullOrWhiteSpace(context.SearchString))
        {
            var search = context.SearchString.Trim();
            statuses = statuses
                .Where(status => status.Contains(search, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        return statuses
            .OrderBy(status => status)
            .Select(status => new DataSourceItem(status, status));
    }
}
