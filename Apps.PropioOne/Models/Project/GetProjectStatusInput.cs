using Blackbird.Applications.Sdk.Common;

namespace Apps.PropioOne.Models.Project
{
    public class GetProjectStatusInput
    {
        [Display("Project ID")]
        public string ProjectId { get; set; } = default!;
    }
}
