using Blackbird.Applications.Sdk.Common.Dictionaries;

namespace Apps.PropioOne.DataHandlers.Static
{
    public class QualityEstimationTypeHandler : IStaticDataSourceHandler
    {
        public Dictionary<string, string> GetData() => new()
        {
            { "basic", "Basic quality estimation" },
            { "comet", "COMET-based quality estimation" }
        };
    }
}
