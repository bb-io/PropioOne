using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.PropioOne.Models.File
{
    public class DownloadAllTranslatedFilesResponse
    {
        [Display("Files")]
        public IEnumerable<FileReference> Files { get; set; } = Enumerable.Empty<FileReference>();

        [Display("ZIP file")]
        public FileReference? ZipFile { get; set; }
    }
}
