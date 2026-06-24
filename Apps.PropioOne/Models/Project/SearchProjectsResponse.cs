using Blackbird.Applications.Sdk.Common;
using Newtonsoft.Json;

namespace Apps.PropioOne.Models.Project;

public class SearchProjectsResponse
{
    [Display("Total count")]
    public int TotalCount { get; set; }

    [Display("Items")]
    public IEnumerable<SearchProjectItem> Items { get; set; } = Enumerable.Empty<SearchProjectItem>();
}

public class SearchProjectsPageResponse
{
    [JsonProperty("items")]
    public IEnumerable<SearchProjectItem> Items { get; set; } = Enumerable.Empty<SearchProjectItem>();

    [JsonProperty("totalCount")]
    public int TotalCount { get; set; }
}

public class SearchProjectItem
{
    [JsonProperty("projectNumber")]
    [Display("Order ID")]
    public int ProjectNumber { get; set; }

    [JsonProperty("projectName")]
    [Display("Order name")]
    public string ProjectName { get; set; } = default!;

    [JsonProperty("customerNumber")]
    [Display("Customer number")]
    public int CustomerNumber { get; set; }

    [JsonProperty("projectType")]
    [Display("Order type")]
    public string ProjectType { get; set; } = default!;

    [JsonProperty("tmsReferenceNumber")]
    [Display("TMS reference number")]
    public string? TmsReferenceNumber { get; set; }

    [JsonProperty("projectStatus")]
    [Display("Order status")]
    public string ProjectStatus { get; set; } = default!;

    [JsonProperty("projectOriginator")]
    [Display("Order originator")]
    public string? ProjectOriginator { get; set; }

    [JsonProperty("createdOn")]
    [Display("Created on")]
    public DateTime CreatedOn { get; set; }
}
