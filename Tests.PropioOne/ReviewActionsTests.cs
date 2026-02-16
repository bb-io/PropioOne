using Tests.PropioOne.Base;

namespace Tests.PropioOne
{
    [TestClass]
    public class ReviewActionsTests : TestBase
    {
        [TestMethod]
        public async Task ReviewText_works()
        {
            var action = new Apps.PropioOne.Actions.ReviewActions(InvocationContext, FileManager);
            var response = await action.ReviewText(new()
            {
                TargetLanguage = "es-ES",
                TargetText = "¡Hola mundo!",
                SourceLanguage = "en-US",
                SourceText = "Hello world!"
            });
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(response);
            Console.WriteLine(json);
            Assert.IsNotNull(response);
        }

        [TestMethod]
        public async Task Review_blackbird_strategy_works()
        {
            var action = new Apps.PropioOne.Actions.ReviewActions(InvocationContext, FileManager);
            var response = await action.Review(new()
            {
                TargetLanguage = "es-ES",
                SourceLanguage = "en-US",
                ReviewStrategy = "blackbird",
                File = new Blackbird.Applications.Sdk.Common.Files.FileReference { Name = "taus_translated.xliff" }
            });
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(response);
            Console.WriteLine(json);
            Assert.IsNotNull(response);
        }

        [TestMethod]
        public async Task Review_propio_strategy_works()
        {
            var action = new Apps.PropioOne.Actions.ReviewActions(InvocationContext, FileManager);
            var response = await action.Review(new()
            {
                TargetLanguage = "es-ES",
                SourceLanguage = "en-US",
                ReviewStrategy = "propio",
                File = new Blackbird.Applications.Sdk.Common.Files.FileReference { Name= "340613_source.html" },
                TargetFile = new Blackbird.Applications.Sdk.Common.Files.FileReference { Name = "340613_target.html" }
            });
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(response);
            Console.WriteLine(json);
            Assert.IsNotNull(response);
        }
    }
}
