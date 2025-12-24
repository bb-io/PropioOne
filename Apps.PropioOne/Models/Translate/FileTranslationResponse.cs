using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.PropioOne.Models.Translate
{
    public class FileTranslationResponse
    {
        [Display("Translated file")]
        public FileReference File { get; set; } = default!;
    }
}
