using Newtonsoft.Json;

namespace Apps.PropioOne.Models.Edit
{
    public class GetApeSegmentsResponse
    {
        [JsonProperty("ape")]
        public List<Dictionary<string, ApeSegmentDto>> Ape { get; set; } = [];
    }

    public class ApeSegmentDto
    {
        [JsonProperty("ape")]
        public string? Ape { get; set; }

        [JsonProperty("source")]
        public string? Source { get; set; }

        [JsonProperty("target")]
        public string? Target { get; set; }
    }
}
