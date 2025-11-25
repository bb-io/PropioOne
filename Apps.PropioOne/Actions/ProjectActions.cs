using Apps.PropioOne.Models.File;
using Apps.PropioOne.Models.Project;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using RestSharp;
using System.IO.Compression;

namespace Apps.PropioOne.Actions;

[ActionList("Project")]
public class ProjectActions(InvocationContext invocationContext, IFileManagementClient fileManagement) : PropioOneInvocable(invocationContext)
{
    [Action("Create project", Description = "Creates project")]
    public async Task<CreateProjectResponse> CreateProject([ActionParameter] CreateProjectInput input)
    {
        using var sourceStream = await fileManagement.DownloadAsync(input.SourceFile);

        var uploadRequest = new RestRequest("/api/v1/project/file/source", Method.Post)
        {
            AlwaysMultipartFormData = true
        };

        byte[] fileBytes;
        using (var ms = new MemoryStream())
        {
            await sourceStream.CopyToAsync(ms);
            fileBytes = ms.ToArray();
        }

        uploadRequest.AddFile("FileToUpload", fileBytes, input.SourceFile.Name);

        var uploadResponse =
            await Client.ExecuteWithErrorHandling<UploadSourceFileResponse>(uploadRequest);

        var sourceFileNumber = uploadResponse.SourceFileNumber;
        if (sourceFileNumber <= 0)
            throw new PluginApplicationException("Propio did not return a valid sourceFileNumber for uploaded source file.");

        DateTime? requestedDueDate = null;
        if (input.DueDate.HasValue)
            requestedDueDate = input.DueDate.Value;

        var attributes = new List<ProjectAttribute>();
        if (!string.IsNullOrWhiteSpace(input.ReferenceNumber))
        {
            attributes.Add(new ProjectAttribute
            {
                Name = "ReferenceNumber",
                Value = input.ReferenceNumber!
            });
        }

        var projectType = string.IsNullOrWhiteSpace(input.ProjectType)
            ? "Standard"
            : input.ProjectType!;

        var translationFileType = string.IsNullOrWhiteSpace(input.TranslationFileType)
            ? "Form"
            : input.TranslationFileType!;

        var createBody = new CreateProjectRequest
        {
            ProjectType = projectType,
            TranslationFileType = translationFileType,
            ProjectName = input.ProjectName,
            Notes = input.Instructions,

            RequestedDueDate = requestedDueDate,
            SourceLanguage = input.SourceLanguageCode,

            SourceFiles = new[]
            {
            new SourceFileRequestModel
            {
                SourceFileNumber = sourceFileNumber,
                TargetLanguages = input.TargetLanguageCodes,
                PageCount = null
            }
        },

            Attributes = attributes.Any() ? attributes : null,

            ProjectOriginator = null,
            DeliverableFormats = null,
            WorkflowName = null,
            CreatedBy = null,
            RequesterName = null
        };

        var createRequest = new RestRequest("/api/v1/project/create", Method.Post);
        createRequest.AddJsonBody(createBody);

        var createResponse =
            await Client.ExecuteWithErrorHandling<CreateProjectResponse>(createRequest);

        if (!string.IsNullOrWhiteSpace(createResponse.ProjectId))
        {
            var statusRequest = new RestRequest(
                $"/api/v1/project/{createResponse.ProjectId}/status",
                Method.Get);

            var statusResponse =
                await Client.ExecuteWithErrorHandling<ProjectStatusResponse>(statusRequest);

            createResponse.Project = statusResponse;

            if (!string.IsNullOrWhiteSpace(statusResponse.ProjectStatus))
                createResponse.Status = statusResponse.ProjectStatus;
        }

        return createResponse;
    }

    [Action("Get project", Description = "Gets detailed status information for a project")]
    public async Task<ProjectStatusResponse> GetProject([ActionParameter] GetProjectStatusInput input)
    {
        if (string.IsNullOrWhiteSpace(input.ProjectId))
            throw new PluginApplicationException("Project ID cannot be empty.");

        var statusRequest = new RestRequest($"/api/v1/project/{input.ProjectId}/status", Method.Get);

        var statusResponse =
            await Client.ExecuteWithErrorHandling<ProjectStatusResponse>(statusRequest);

        return statusResponse;
    }

    [Action("Download translated target file", Description = "Downloads a translated file for the specified project, file and target language")]
    public async Task<DownloadTranslatedFileResponse> DownloadTranslatedTargetFile([ActionParameter] DownloadTranslatedFileInput input)
    {
        var request = new RestRequest(
       $"/api/v1/project/{input.ProjectId}/file/source/{input.FileId}/{input.TargetLanguageCode}/content",
       Method.Get);

        var response = await Client.ExecuteWithErrorHandling(request);

        if (response.RawBytes == null || response.RawBytes.Length == 0)
            throw new PluginApplicationException("Propio returned an empty file when downloading translated target.");

        var fileName = GetFileNameFromContentDisposition(response.Headers)
                       ?? $"{input.ProjectId}_{input.TargetLanguageCode}.bin";

        var extensionContentType = GetContentTypeFromExtension(fileName);

        var responseContentType = string.IsNullOrWhiteSpace(response.ContentType)
            ? null
            : response.ContentType!;

        string contentType;
        if (extensionContentType != "application/octet-stream")
        {
            contentType = extensionContentType;
        }
        else if (!string.IsNullOrWhiteSpace(responseContentType))
        {
            contentType = responseContentType;
        }
        else
        {
            contentType = "application/octet-stream";
        }

        using var ms = new MemoryStream(response.RawBytes);
        var fileRef = await fileManagement.UploadAsync(ms, contentType, fileName);

        return new DownloadTranslatedFileResponse
        {
            File = fileRef
        };
    }

    [Action("Download all translated files",Description = "Downloads all translated files for the specified project and target language. Unzips by default and returns individual files.")]
    public async Task<DownloadAllTranslatedFilesResponse> DownloadAllTranslatedFiles([ActionParameter] DownloadAllTranslatedFilesInput input)
    {
        var request = new RestRequest($"/api/v1/project/{input.ProjectId}/target/{input.TargetLanguageCode}/zip",Method.Get);

        var response = await Client.ExecuteWithErrorHandling(request);

        if (response.RawBytes == null || response.RawBytes.Length == 0)
            throw new PluginApplicationException("Empty ZIP returned when downloading all translated files.");

        var zipBytes = response.RawBytes;

        var files = new List<FileReference>();
        FileReference? zipFileRef = null;

        if (input.IncludeZipFile == true)
        {
            using var zipStreamForUpload = new MemoryStream(zipBytes);
            zipFileRef = await fileManagement.UploadAsync(
                zipStreamForUpload,
                "application/zip",
                $"{input.ProjectId}_{input.TargetLanguageCode}.zip");
        }

        using var zipStream = new MemoryStream(zipBytes);
        using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read, leaveOpen: false);

        foreach (var entry in archive.Entries)
        {
            if (string.IsNullOrEmpty(entry.Name))
                continue;

            await using var entryStream = entry.Open();
            using var ms = new MemoryStream();
            await entryStream.CopyToAsync(ms);
            ms.Position = 0;

            var contentType = GetContentTypeFromExtension(entry.Name);
            var fileRef = await fileManagement.UploadAsync(ms, contentType, entry.Name);
            files.Add(fileRef);
        }

        if (!files.Any())
        {
            throw new PluginApplicationException(
                $"ZIP for project {input.ProjectId}, language {input.TargetLanguageCode} did not contain any files.");
        }

        return new DownloadAllTranslatedFilesResponse
        {
            Files = files,
            ZipFile = zipFileRef
        };
    }

    [Action("Cancel project", Description = "Cancels a project in Propio")]
    public async Task CancelProject([ActionParameter] CancelProjectInput input)
    {
        if (string.IsNullOrWhiteSpace(input.ProjectId))
            throw new PluginMisconfigurationException("Project ID cannot be empty.");

        var request = new RestRequest($"/api/v1/project/{input.ProjectId}", Method.Delete);

        var apiResponse = await Client.ExecuteWithErrorHandling(request);
    }

    //helpers

    private static string GetContentTypeFromExtension(string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();

        return ext switch
        {
            ".txt" => "text/plain",
            ".htm" or ".html" => "text/html",
            ".pdf" => "application/pdf",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xls" => "application/vnd.ms-excel",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".json" => "application/json",
            ".xml" => "application/xml",
            ".xlf" or ".xliff" => "application/xliff+xml",
            ".zip" => "application/zip",
            _ => "application/octet-stream"
        };
    }

    private static string? GetFileNameFromContentDisposition(IEnumerable<HeaderParameter>? headers)
    {
        var header = headers?
         .FirstOrDefault(h =>
             string.Equals(h.Name, "Content-Disposition", StringComparison.OrdinalIgnoreCase));

        var value = header?.Value?.ToString();
        if (string.IsNullOrEmpty(value))
            return null;

        const string key = "filename=";
        var index = value.IndexOf(key, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
            return null;

        var fileName = value[(index + key.Length)..].Trim('\"', ' ', ';');
        return string.IsNullOrWhiteSpace(fileName) ? null : fileName;
    }
}
