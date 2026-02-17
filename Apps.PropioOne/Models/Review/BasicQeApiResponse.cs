using Newtonsoft.Json;

namespace Apps.PropioOne.Models.Review
{
    public class BasicQeApiResponse
    {
        [JsonProperty("basicQE")]
        public List<string>? BasicQE { get; set; }

        [JsonProperty("basicQEscore_AVG")]
        public double? BasicQEscoreAVG { get; set; }
    }
}
