using Apps.PropioOne.Actions;
using Blackbird.Applications.Sdk.Common.Files;
using Tests.PropioOne.Base;

namespace Tests.PropioOne;

[TestClass]
public class MachineTranslationActionsTests : TestBase
{
    [TestMethod]
    public async Task TranslateText_works()
    {

        var action = new MachineTranslation(InvocationContext, FileManager);
        var response = await action.TranslateText(new()
        {
            ProjectId = "1849777",
            SourceLanguage = "en-US",
            TargetLanguage = "es-ES",
            Text = "Hello world, brother",
            Domain = "General Vocabulary",
            Provider = "Microsoft"
        });

        var json = Newtonsoft.Json.JsonConvert.SerializeObject(response);
        Console.WriteLine(json);
        Assert.IsNotNull(response);
    }

    [TestMethod]
    public async Task Translate_propio_strategy_works()
    {

        var action = new MachineTranslation(InvocationContext, FileManager);
        var response = await action.Translate(new()
        {
            ProjectId = "1849777",
            SourceLanguage = "en-US",
            TargetLanguage = "es-ES",
            Domain = "General Vocabulary",
            Provider = "Microsoft",
            File = new()
            {
                Name = "340613_source.html"
            },
            FileTranslationStrategy = "propio"
            //OutputFileHandling = "original"
        });

        var json = Newtonsoft.Json.JsonConvert.SerializeObject(response);
        Console.WriteLine(json);
        Assert.IsNotNull(response);
    }

    [TestMethod]
    public async Task Translate_blackbird_strategy_works()
    {

        var action = new MachineTranslation(InvocationContext, FileManager);
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
            FileTranslationStrategy = "blackbird"
            //OutputFileHandling = "original"
        });

        var json = Newtonsoft.Json.JsonConvert.SerializeObject(response);
        Console.WriteLine(json);
        Assert.IsNotNull(response);
    }

    [TestMethod]
    public async Task Edit_works()
    {
        var action = new MachineTranslation(InvocationContext, FileManager);

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
        var action = new MachineTranslation(InvocationContext, FileManager);

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

    [TestMethod]
    public async Task ReviewText_works()
    {
        var action = new Apps.PropioOne.Actions.MachineTranslation(InvocationContext, FileManager);
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
        var action = new Apps.PropioOne.Actions.MachineTranslation(InvocationContext, FileManager);
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
        var action = new Apps.PropioOne.Actions.MachineTranslation(InvocationContext, FileManager);
        var response = await action.Review(new()
        {
            TargetLanguage = "es-ES",
            SourceLanguage = "en-US",
            ReviewStrategy = "propio",
            File = new Blackbird.Applications.Sdk.Common.Files.FileReference { Name = "340613_source.html" },
            TargetFile = new Blackbird.Applications.Sdk.Common.Files.FileReference { Name = "340613_target.html" }
        });
        var json = Newtonsoft.Json.JsonConvert.SerializeObject(response);
        Console.WriteLine(json);
        Assert.IsNotNull(response);
    }
}

