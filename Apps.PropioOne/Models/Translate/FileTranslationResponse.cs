using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.SDK.Blueprints.Interfaces.Translate;

namespace Apps.PropioOne.Models.Translate
{
    public class FileTranslationResponse : ITranslateFileOutput
    {
        [Display("Translated file")]
        public FileReference File { get; set; } = default!;
    }
}
