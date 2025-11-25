using Blackbird.Applications.Sdk.Common;

namespace Apps.PropioOne.Models.Project
{
    public class CancelProjectInput
    {
        [Display("Project ID")]
        public string ProjectId { get; set; } = default!;

        [Display("Reason")]
        public string? Reason { get; set; }
    }
}
