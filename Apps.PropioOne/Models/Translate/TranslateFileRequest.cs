using Apps.PropioOne.DataHandlers.Static;
using Apps.PropioOne.Handlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.SDK.Blueprints.Handlers;
using Blackbird.Applications.SDK.Blueprints.Interfaces.Translate;

namespace Apps.PropioOne.Models.Translate
{
    public class TranslateFileRequest : ITranslateFileInput
    {
        [Display("File")]
        public FileReference File { get; set; } = default!;

        [Display("Order ID")]
        public string ProjectId { get; set; }

        [Display("Source language")]
        [DataSource(typeof(LanguageDataHandler))]
        public string SourceLanguage { get; set; }

        [Display("Target language")]
        [DataSource(typeof(LanguageDataHandler))]
        public string TargetLanguage{ get; set; }

        [Display("Domain")]
        public string Domain { get; set; }

        [Display("Provider")]
        public string Provider { get; set; }

        [Display("Client application")]
        public string? ClientApplication { get; set; }

        [Display("Document name")]
        public string? DocumentName { get; set; }

        [Display("Job ID", Description = "Optional. If empty, a random job id will be generated.")]
        public int? JobId { get; set; }

        [Display("File translation strategy", Description = "blackbird (segment-based) or propio (native document translation)")]
        [StaticDataSource(typeof(FileTranslationStrategyHandler))]
        public string? FileTranslationStrategy { get; set; }

        [Display("Output file handling", Description = "original = return original format; otherwise returns XLIFF")]
        [StaticDataSource(typeof(ProcessFileFormatHandler))]
        public string? OutputFileHandling { get; set; }
    }
}
