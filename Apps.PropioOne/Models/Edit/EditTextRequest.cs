using Apps.PropioOne.Handlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.SDK.Blueprints.Interfaces.Edit;

namespace Apps.PropioOne.Models.Edit
{
    public class EditTextRequest : IEditTextInput
    {
        [Display("Source text")]
        public string SourceText { get; set; }

        [Display("Target text")]
        public string TargetText { get; set; }

        [Display("Target language")]
        [DataSource(typeof(LanguageDataHandler))]
        public string TargetLanguage { get; set; }

        [Display("Source language")]
        [DataSource(typeof(LanguageDataHandler))]
        public string SourceLanguage { get; set; }

        [Display("Domain")]
        public string? Domain { get; set; }
    }
}
