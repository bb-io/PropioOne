using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.PropioOne.Models.File
{
    public class DownloadTranslatedFileResponse
    {
        [Display("File")]
        public FileReference File { get; set; } = default!;
    }
}
