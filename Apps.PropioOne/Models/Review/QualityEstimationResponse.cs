using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.SDK.Blueprints.Interfaces.Review;

namespace Apps.PropioOne.Models.Review
{
    public class QualityEstimationResponse : IReviewFileOutput
    {
        public FileReference File { get; set; }

        [Display("Total segments processed")]
        public int TotalSegmentsProcessed { get; set; }

        [Display("Total segments finalized")]
        public int TotalSegmentsFinalized { get; set; }

        [Display("Total segments under threshold")]
        public int TotalSegmentsUnderThreshhold { get; set; }

        [Display("Avarage metric")]
        public float AverageMetric { get; set; }

        [Display("Percentage of segments under threshold")]
        public float PercentageSegmentsUnderThreshhold { get; set; }
    }
}
