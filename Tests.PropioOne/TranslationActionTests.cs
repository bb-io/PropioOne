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
                ProjectId = "1849777",
                SourceLanguage = "es-ES",
                TargetLanguage = "en-US",
                Text = "Hola mundo",
                Domain= "General Vocabulary",
                Provider= "Microsoft"
            });

            var json = Newtonsoft.Json.JsonConvert.SerializeObject(response);

            Assert.IsNotNull(response);
        }

    }
}
