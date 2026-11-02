using Newtonsoft.Json;

namespace Apps.PropioOne.Models.Project;

public class UploadReferenceFileResponse
{
    [JsonProperty("referenceFileNumber")]
    public int ReferenceFileNumber { get; set; }
}
