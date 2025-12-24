using Blackbird.Applications.Sdk.Common;
using Newtonsoft.Json;

namespace Apps.PropioOne.Models.Project
{
    public class CreateProjectResponse
    {
        [Display("Project ID")]
        [JsonProperty("projectNumber")]
        public string ProjectId { get; set; } = default!;

        [Display("Status")]
        [JsonProperty("status")]
        public string Status { get; set; } = default!;

        [Display("Project details")]
        public ProjectStatusResponse? Project { get; set; }
    }
}
