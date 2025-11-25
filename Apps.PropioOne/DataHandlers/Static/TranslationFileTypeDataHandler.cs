using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.PropioOne.DataHandlers.Static
{
    public class TranslationFileTypeDataHandler : IStaticDataSourceItemHandler
    {
        public IEnumerable<DataSourceItem> GetData()
        {
            return new List<DataSourceItem>
            {
                new DataSourceItem("Form", "Form"),
                new DataSourceItem("Full", "Full")
            };
        }
    }
}
