using Blackbird.Applications.Sdk.Common.Dictionaries;

namespace Apps.PropioOne.DataHandlers.Static
{
    public class FileTranslationStrategyHandler : IStaticDataSourceHandler
    {
        public Dictionary<string, string> GetData()
       => new()
       {
            { "blackbird", "Blackbird (segment-based)" },
            { "propio", "Propio (native document translation)" }
       };
    }
}
