using Newtonsoft.Json;

namespace Apps.PropioOne.Models.Translate
{
    public class TranslateTextRequest
    {
        public int? ClientId { get; set; }
        public int? ProjectId { get; set; }
        public string? ClientApplication { get; set; }
        public string? DocumentName { get; set; }
        public TranslationDirection TranslationDirection { get; set; } = default!;
        public string? Domain { get; set; }
        public string? Provider { get; set; }
        public List<string> OriginalText { get; set; } = new();
    }
    public class TranslationDirection
    {
        public string SourceLanguage { get; set; } = default!;
        public string TargetLanguage { get; set; } = default!;
    }
    public class TextTranslationApiResponse
    {
        [JsonProperty("translatedTexts")]
        public List<TranslatedTextItem>? TranslatedTexts { get; set; }
    }

    public class TranslatedTextItem
    {
        [JsonProperty("originalText")]
        public string? OriginalText { get; set; }

        [JsonProperty("translatedText")]
        public string? TranslatedText { get; set; }
    }

    public class TranslateTextResponseModel
    {
        public int JobId { get; set; }
        public List<TranslatedSegment> TranslatedSegments { get; set; } = new();
    }

    public class TranslatedSegment
    {
        public string? Text { get; set; }
    }
}
