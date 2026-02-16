using Apps.PropioOne.Handlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.SDK.Blueprints.Interfaces.Review;

namespace Apps.PropioOne.Models.Review
{
    public class ReviewTextRequest : IReviewTextInput
    {
        [Display("Source text")]
        public string SourceText { get; set; } = string.Empty;

        [Display("Target text")]
        public string TargetText { get; set; } = string.Empty;

        [Display("Domain")]
        public string? Domain { get; set; } = "General Vocabulary";

        [Display("Source language")]
        [DataSource(typeof(LanguageDataHandler))]
        public string SourceLanguage { get; set; }

        [Display("Target language")]
        [DataSource(typeof(LanguageDataHandler))]
        public string TargetLanguage { get; set; }
    }
}
