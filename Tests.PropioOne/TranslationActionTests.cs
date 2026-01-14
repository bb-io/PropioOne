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
                SourceLanguage = "en-US",
                TargetLanguage = "es-ES",
                Text = "Hello world, brother",
                Domain= "General Vocabulary",
                Provider= "Microsoft"
            });

            var json = Newtonsoft.Json.JsonConvert.SerializeObject(response);
            Console.WriteLine(json);
            Assert.IsNotNull(response);
        }

        [TestMethod]
        public async Task Translate_works()
        {

            var action = new TranslationActions(InvocationContext, FileManager);
            var response = await action.Translate(new()
            {
                ProjectId = "1849777",
                SourceLanguage = "en-US",
                TargetLanguage = "es-ES",
                Domain = "General Vocabulary",
                Provider = "Microsoft",
                File = new()
                {
                    Name = "taus.xliff"
                },
                OutputFileHandling = "original"
            });

            var json = Newtonsoft.Json.JsonConvert.SerializeObject(response);
            Console.WriteLine(json);
            Assert.IsNotNull(response);
        }
    }
}
