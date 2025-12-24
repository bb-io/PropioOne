using Apps.PropioOne.DataHandlers.Static;
using Apps.PropioOne.Handlers;
using Apps.PropioOne.Handlers.Static;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.PropioOne.Models.Project
{
    public class CreateProjectInput
    {
        [Display("Project name")]
        public string ProjectName { get; set; } = default!;

        [Display("Project type")]
        [StaticDataSource(typeof(ProjectTypeDataHandler))]
        public string? ProjectType { get; set; }

        [Display("Translation file type")]
        [StaticDataSource(typeof(TranslationFileTypeDataHandler))]
        public string? TranslationFileType { get; set; }

        [Display("Source language")]
        [DataSource(typeof(LanguageDataHandler))]
        public string SourceLanguageCode { get; set; } = default!;

        [Display("Target languages")]
        [DataSource(typeof(LanguageDataHandler))]
        public IEnumerable<string> TargetLanguageCodes { get; set; } = Enumerable.Empty<string>();

        [Display("Due date")]
        public DateTime? DueDate { get; set; }

        [Display("Reference number")]
        public string? ReferenceNumber { get; set; }

        [Display("Instructions")]
        public string? Instructions { get; set; }

        [Display("Source file")]
        public FileReference SourceFile { get; set; } = default!;
    }
}
