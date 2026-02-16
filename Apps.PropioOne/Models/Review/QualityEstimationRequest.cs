using Apps.PropioOne.DataHandlers.Static;
using Apps.PropioOne.Handlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.SDK.Blueprints.Handlers;
using Blackbird.Applications.SDK.Blueprints.Interfaces.Review;

namespace Apps.PropioOne.Models.Review
{
    public class QualityEstimationRequest : IReviewFileInput
    {
        [Display("Review strategy", Description = "blackbird (segment-based) or propio (native file-to-file QE)")]
        [StaticDataSource(typeof(FileTranslationStrategyHandler))]
        public string ReviewStrategy { get; set; }

        [Display("Source file", Description = "Used for both strategies. For blackbird it must be a parsed/processable file (XLIFF). For propio it is the source file.")]
        public FileReference File { get; set; }

        [Display("Target file", Description = "Required only for propio strategy")]
        public FileReference? TargetFile { get; set; }

        [Display("Score threshold", Description = "All segments above this score will automatically be finalized (0..1)")]
        public double? Threshold { get; set; }

        [Display("Output file handling", Description = "original = return original format; otherwise returns XLIFF")]
        [StaticDataSource(typeof(ProcessFileFormatHandler))]
        public string? OutputFileHandling { get; set; }

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
