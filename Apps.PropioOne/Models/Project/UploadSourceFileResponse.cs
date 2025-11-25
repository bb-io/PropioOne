using Newtonsoft.Json;

namespace Apps.PropioOne.Models.Project
{
    public class UploadSourceFileResponse
    {
        [JsonProperty("sourceFileNumber")]
        public int SourceFileNumber { get; set; } = default!;
    }
}
