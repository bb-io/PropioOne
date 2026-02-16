using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.SDK.Blueprints.Interfaces.Review;

namespace Apps.PropioOne.Models.Review
{
    public class QualityEstimationResponse : IReviewFileOutput
    {
        public FileReference File { get; set; }
        public int TotalSegmentsProcessed { get; set; }
        public int TotalSegmentsFinalized { get; set; }
        public int TotalSegmentsUnderThreshhold { get; set; }
        public float AverageMetric { get; set; }
        public float PercentageSegmentsUnderThreshhold { get; set; }
    }
}
