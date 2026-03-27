using Apps.PropioOne.Models.Project;
using Apps.PropioOne.Polling.Models;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.Sdk.Common.Polling;
using RestSharp;

namespace Apps.PropioOne.Polling;

[PollingEventList]
public class OrderPollingList(InvocationContext invocationContext) : PropioOneInvocable(invocationContext)
{
    [PollingEvent("On order status changed", "Triggered when an order status changes. Optionally filters by a specific status.")]
    public async Task<PollingEventResponse<OrderStatusMemory, ProjectStatusResponse>> OnOrderStatusChanged(
        PollingEventRequest<OrderStatusMemory> request,
        [PollingEventParameter] OrderStatusPollingInput input)
    {
        var statusRequest = new RestRequest($"/api/v1/project/{input.ProjectId}/status", Method.Get);
        var order = await Client.ExecuteWithErrorHandling<ProjectStatusResponse>(statusRequest);

        var currentStatus = order.ProjectStatus;

        if (request.Memory is null)
        {
            return new PollingEventResponse<OrderStatusMemory, ProjectStatusResponse>
            {
                FlyBird = false,
                Memory = new OrderStatusMemory
                {
                    LastStatus = currentStatus
                }
            };
        }

        var changed = !string.Equals(request.Memory.LastStatus, currentStatus, StringComparison.OrdinalIgnoreCase);
        var matchesFilter = string.IsNullOrWhiteSpace(input.ProjectStatus) ||
                            string.Equals(input.ProjectStatus, currentStatus, StringComparison.OrdinalIgnoreCase);

        return new PollingEventResponse<OrderStatusMemory, ProjectStatusResponse>
        {
            FlyBird = changed && matchesFilter,
            Result = changed && matchesFilter ? order : null,
            Memory = new OrderStatusMemory
            {
                LastStatus = currentStatus
            }
        };
    }
}
