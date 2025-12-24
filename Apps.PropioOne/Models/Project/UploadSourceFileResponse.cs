using Newtonsoft.Json;

namespace Apps.PropioOne.Models.Project
{
    public class UploadSourceFileResponse
    {
        [JsonProperty("sourceFileNumber")]
        public string SourceFileNumber { get; set; } = default!;
    }
}
