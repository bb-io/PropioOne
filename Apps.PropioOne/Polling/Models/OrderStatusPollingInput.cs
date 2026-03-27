using Apps.PropioOne.Handlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.PropioOne.Polling.Models;

public class OrderStatusPollingInput
{
    [Display("Order ID")]
    public string ProjectId { get; set; } = default!;

    [Display("Order status")]
    [DataSource(typeof(ProjectStatusDataHandler))]
    public string? ProjectStatus { get; set; }
}
