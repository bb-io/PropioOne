using Blackbird.Applications.Sdk.Common;
using Newtonsoft.Json;

namespace Apps.PropioOne.Models.Project;

public class AcceptQuoteInput
{
    [Display("Order ID")]
    public string ProjectId { get; set; } = default!;

    [Display("Quote number")]
    public string QuoteNumber { get; set; } = default!;
}

public class RejectQuoteInput
{
    [Display("Order ID")]
    public string ProjectId { get; set; } = default!;

    [Display("Quote number")]
    public string QuoteNumber { get; set; } = default!;

    [Display("Reject reason")]
    public string RejectReason { get; set; } = default!;
}

public class QuoteStatusUpdateRequest
{
    [JsonProperty("quoteNumber")]
    public long QuoteNumber { get; set; }

    [JsonProperty("status")]
    public string Status { get; set; } = default!;

    [JsonProperty("rejectReason")]
    public string? RejectReason { get; set; }
}

public class QuoteStatusUpdateResponse
{
    [JsonProperty("message")]
    [Display("Message")]
    public string? Message { get; set; }

    [JsonProperty("success")]
    [Display("Success")]
    public bool Success { get; set; }
}
