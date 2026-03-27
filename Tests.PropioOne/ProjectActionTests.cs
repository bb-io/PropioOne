using Apps.PropioOne.Actions;
using Tests.PropioOne.Base;

namespace Tests.PropioOne;

[TestClass]
public class ProjectActionTests : TestBase
{
    [TestMethod]
    public async Task CreateProject_works()
    {
       var action = new ProjectActions(InvocationContext, FileManager);

        var response = await action.CreateProject(new()
        {
            ProjectName = "Test Project 1",
            SourceLanguageCode = "en",
            TargetLanguageCodes = new[] { "es" },
            SourceFile = new Blackbird.Applications.Sdk.Common.Files.FileReference { Name= "taus.xliff" },
            DueDate = DateTime.UtcNow.AddDays(7),
            ProjectType= "Standard",
            TranslationFileType = "Form",
            ReferenceNumber = "REF123",
            Instructions = "Please translate this document."
        });

        var json = Newtonsoft.Json.JsonConvert.SerializeObject(response);
        Console.WriteLine(json);
        Assert.IsNotNull(response);
    }

    [TestMethod]
    public async Task GetProjectStatus_works()
    {
        var action = new ProjectActions(InvocationContext, FileManager);

        var response = await action.GetProject(new() { ProjectId = "1849777" });

        var json = Newtonsoft.Json.JsonConvert.SerializeObject(response);
        Console.WriteLine(json);
        Assert.IsNotNull(response);
    }

    [TestMethod]
    public async Task DownloadTargetFile_works()
    {
        var action = new ProjectActions(InvocationContext, FileManager);

        var response = await action.DownloadTranslatedTargetFile(new() { ProjectId= "650335", FileId= "668091" , TargetLanguageCode = "en" });

        var json = Newtonsoft.Json.JsonConvert.SerializeObject(response);
        Console.WriteLine(json);
        Assert.IsNotNull(response);
    }

    [TestMethod]
    public async Task DownloadTargetFiles_works()
    {
        var action = new ProjectActions(InvocationContext, FileManager);

        var response = await action.DownloadAllTranslatedFiles(new() { ProjectId = "650335", TargetLanguageCode = "en" });

        var json = Newtonsoft.Json.JsonConvert.SerializeObject(response);
        Console.WriteLine(json);
        Assert.IsNotNull(response);
    }

    [TestMethod]
    public async Task CancelProject_works()
    {
        var action = new ProjectActions(InvocationContext, FileManager);

        await action.CancelProject(new() { ProjectId = "650335"});

        Assert.IsTrue(true);
    }
}
