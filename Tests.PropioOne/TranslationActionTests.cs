using Apps.PropioOne.Actions;
using Tests.PropioOne.Base;

namespace Tests.PropioOne
{
    [TestClass]
    public class TranslationActionTests : TestBase
    {
        [TestMethod]
        public async Task TranslateText_works()
        {

            var action = new TranslationActions(InvocationContext, FileManager);
            var response = await action.TranslateText(new()
            {
                SourceLanguage = "en",
                TargetLanguage = "es",
                Text = "Hello, world!",
                IsHtml = false
            });

            var json = Newtonsoft.Json.JsonConvert.SerializeObject(response);

            Assert.IsNotNull(response);
        }

    }
}
