using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.SDK.Blueprints.Interfaces.Edit;

namespace Apps.PropioOne.Models.Edit
{
    public class EditFileResponse : IEditFileOutput
    {
        [Display("Edited file")]
        public FileReference File { get; set; }

        [Display("Total segments reviewed")]
        public int TotalSegmentsReviewed { get; set; }

        [Display("Total segments updated")]
        public int TotalSegmentsUpdated { get; set; }
    }
}
