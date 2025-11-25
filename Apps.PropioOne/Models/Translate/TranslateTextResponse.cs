using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.SDK.Blueprints.Interfaces.Translate;

namespace Apps.PropioOne.Models.Translate
{
    public class TranslateTextResponse : ITranslateTextOutput
    {
        [Display("Source language")]
        public string SourceLanguageCode { get; set; } = default!;

        [Display("Target language")]
        public string TargetLanguageCode { get; set; } = default!;

        [Display("Source text")]
        public string SourceText { get; set; } = default!;

        [Display("Translated text")]
        public string TranslatedText { get; set; } = default!;
    }
}
