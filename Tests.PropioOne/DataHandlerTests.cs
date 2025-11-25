using Apps.PropioOne.DataHandlers.Static;
using Apps.PropioOne.Handlers.Static;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Tests.PropioOne.Base;

namespace Tests.PropioOne;

[TestClass]
public class DataHandlerTests : TestBase
{
    [TestMethod]
    public async Task LanguageDataHandler_works()
    {
       var handler = new Apps.PropioOne.Handlers.LanguageDataHandler(InvocationContext);
        var result = await handler.GetDataAsync(new DataSourceContext
        {
        }, CancellationToken.None);

        foreach (var item in result)
        {
            Console.WriteLine($"{item.DisplayName} - {item.Value}");
        }

        Assert.IsNotNull(result);
        Assert.IsTrue(result.Any());
    }

    [TestMethod]
    public async Task ProjectTypeDataHandler_works()
    {
        var handler = new ProjectTypeDataHandler();
        var result =  handler.GetData();

        foreach (var item in result)
        {
            Console.WriteLine($"{item.DisplayName} - {item.Value}");
        }

        Assert.IsNotNull(result);
        Assert.IsTrue(result.Any());
    }

    [TestMethod]
    public async Task TranslationFileTypeDataHandler_works()
    {
        var handler = new TranslationFileTypeDataHandler();
        var result = handler.GetData();

        foreach (var item in result)
        {
            Console.WriteLine($"{item.DisplayName} - {item.Value}");
        }

        Assert.IsNotNull(result);
        Assert.IsTrue(result.Any());
    }
}
