using Apps.PropioOne.Handlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.PropioOne.Models.File
{
    public class DownloadAllTranslatedFilesInput
    {
        [Display("Project ID")]
        public string ProjectId { get; set; } = default!;

        [Display("Target language")]
        [DataSource(typeof(LanguageDataHandler))]
        public string TargetLanguageCode { get; set; } = default!;

        [Display("Include ZIP file in output")]
        public bool? IncludeZipFile { get; set; }
    }
}
