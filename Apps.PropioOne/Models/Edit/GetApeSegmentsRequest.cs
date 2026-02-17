using Newtonsoft.Json;

namespace Apps.PropioOne.Models.Edit
{
    public class GetApeSegmentsRequest
    {
        [JsonProperty("originalText")]
        public List<string>? OriginalText { get; set; }

        [JsonProperty("translatedText")]
        public List<string>? TranslatedText { get; set; }

        [JsonProperty("sourceFile")]
        public string? SourceFile { get; set; } = "";

        [JsonProperty("targetFile")]
        public string? TargetFile { get; set; } = "";

        [JsonProperty("domain")]
        public string? Domain { get; set; }

        [JsonProperty("sourceLanguage")]
        public string? SourceLanguage { get; set; }

        [JsonProperty("targetLanguage")]
        public string? TargetLanguage { get; set; }
    }
}
