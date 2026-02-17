using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.SDK.Blueprints.Interfaces.Edit;

namespace Apps.PropioOne.Models.Edit
{
    public class EditTextResponse : IEditTextOutput
    {
        [Display("Edited text")]
        public string EditedText { get; set; } 
    }
}
