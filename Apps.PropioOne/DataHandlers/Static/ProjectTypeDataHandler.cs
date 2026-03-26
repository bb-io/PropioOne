using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.PropioOne.Handlers.Static;
public class ProjectTypeDataHandler : IStaticDataSourceItemHandler
{
    public IEnumerable<DataSourceItem> GetData()
    {
        return new List<DataSourceItem>
        {
            new DataSourceItem("Standard", "Standard"),
            new DataSourceItem("RawMT", "Raw MT"),
        };
    }
}
