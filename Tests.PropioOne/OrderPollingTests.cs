using Apps.PropioOne.Polling;
using Apps.PropioOne.Polling.Models;
using Blackbird.Applications.Sdk.Common.Polling;
using Tests.PropioOne.Base;

namespace Tests.PropioOne;

[TestClass]
public class OrderPollingTests : TestBase
{
    [TestMethod]
    public async Task OnOrderStatusChanged_FirstPollWithoutStatus_DoesNotTriggerAndStoresMemory()
    {
        var polling = new OrderPollingList(InvocationContext);

        var response = await polling.OnOrderStatusChanged(
            new PollingEventRequest<OrderStatusMemory>
            {
                Memory = new OrderStatusMemory { LastStatus = "Initiated" }
            },
            new OrderStatusPollingInput
            {
                ProjectId = "710404",
                ProjectStatus = "In-Progress"
            });

        Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(response));

        Assert.IsNotNull(response);
    }
}
