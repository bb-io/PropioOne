using Apps.PropioOne.Handlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.PropioOne.Models.Project;

public class SearchProjectsInput
{
    [Display("Order status")]
    [DataSource(typeof(ProjectStatusDataHandler))]
    public string? Status { get; set; }

    [Display("From date")]
    public DateTime? FromDate { get; set; }

    [Display("To date")]
    public DateTime? ToDate { get; set; }
}
