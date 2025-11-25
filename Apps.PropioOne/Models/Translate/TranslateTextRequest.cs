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
    internal class TextTranslationApiResponse
    {
        public TranslationDirection? TranslationDirection { get; set; }
        public List<string>? OriginalText { get; set; }
        public List<string>? TranslatedText { get; set; }
    }
}
