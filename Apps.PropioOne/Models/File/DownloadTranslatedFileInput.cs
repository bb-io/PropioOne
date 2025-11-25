using Apps.PropioOne.Handlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.PropioOne.Models.File
{
    public class DownloadTranslatedFileInput
    {
        [Display("Project ID")]
        public string ProjectId { get; set; } = default!;

        [Display("File ID")]
        public string FileId { get; set; } = default!;

        [Display("Target language")]
        [DataSource(typeof(LanguageDataHandler))]
        public string TargetLanguageCode { get; set; } = default!;
    }
}
