using Apps.PropioOne.Actions;
using Blackbird.Applications.Sdk.Common.Files;
using Tests.PropioOne.Base;

namespace Tests.PropioOne
{
    [TestClass]
    public class EditActionsTests : TestBase
    {
        [TestMethod]
        public async Task Edit_works()
        {
            var action = new EditActions(InvocationContext, FileManager);

            var result = await action.Edit(new Apps.PropioOne.Models.Edit.EditFileRequest
            {
                File = new FileReference
                {
                    Name = "taus_translated.xliff"

                },
                TargetLanguage = "es-ES",
                SourceLanguage = "en-US",
            });

            Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(result));
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public async Task Edit_text_works()
        {
            var action = new EditActions(InvocationContext, FileManager);

            var result = await action.EditText(new Apps.PropioOne.Models.Edit.EditTextRequest
            {
                TargetLanguage = "es-ES",
                SourceLanguage = "en-US",
                SourceText = "Good day,brother, how are you?",
                TargetText = "Hola, cómo estás"
            });

            Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(result));
            Assert.IsNotNull(result);
        }
    }
}
