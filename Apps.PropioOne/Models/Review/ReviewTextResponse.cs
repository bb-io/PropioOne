using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.SDK.Blueprints.Interfaces.Review;

namespace Apps.PropioOne.Models.Review
{
    public class ReviewTextResponse : IReviewTextOutput
    {
        [Display("Score")]
        public float Score { get; set; }
    }
}
