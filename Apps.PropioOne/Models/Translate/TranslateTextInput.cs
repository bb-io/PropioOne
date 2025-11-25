using Apps.PropioOne.Handlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.SDK.Blueprints.Interfaces.Translate;

namespace Apps.PropioOne.Models.Translate
{
    public class TranslateTextInput : ITranslateTextInput
    {
        [Display("Source language")]
        [DataSource(typeof(LanguageDataHandler))]
        public string SourceLanguage { get; set; } = default!;

        [Display("Target language")]
        [DataSource(typeof(LanguageDataHandler))]
        public string TargetLanguage { get; set; } = default!;

        [Display("Text")]
        public string Text { get; set; } = default!;

        [Display("Text is HTML")]
        public bool? IsHtml { get; set; }

        [Display("Client ID")]
        public int? ClientId { get; set; }

        [Display("Project ID")]
        public int? ProjectId { get; set; }

        [Display("Client application")]
        public string? ClientApplication { get; set; }

        [Display("Document name")]
        public string? DocumentName { get; set; }

        [Display("Domain")]
        public string? Domain { get; set; }

        [Display("Provider")]
        public string? Provider { get; set; }
    }
}
