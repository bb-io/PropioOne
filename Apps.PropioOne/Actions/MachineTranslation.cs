using Apps.PropioOne.Constants;
using Apps.PropioOne.Models.Edit;
using Apps.PropioOne.Models.Review;
using Apps.PropioOne.Models.Translate;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Blueprints;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using Blackbird.Filters.Constants;
using Blackbird.Filters.Enums;
using Blackbird.Filters.Extensions;
using Blackbird.Filters.Transformations;
using Blackbird.Filters.Xliff.Xliff1;
using Blackbird.Filters.Xliff.Xliff2;
using RestSharp;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Apps.PropioOne.Actions;

[ActionList("AI / Machine Translation")]
public class MachineTranslation(InvocationContext invocationContext, IFileManagementClient fileManagement) : PropioOneInvocable(invocationContext)
{
    [BlueprintActionDefinition(BlueprintAction.TranslateText)]
    [Action("Translate text", Description = "Localize the text provided using MT.")]
    public async Task<TranslateTextResponse> TranslateText([ActionParameter] TranslateTextInput input)
    {
        string? clientIdRaw = invocationContext.AuthenticationCredentialsProviders.FirstOrDefault(x => x.KeyName == CredsNames.ClientId)?.Value;

        if (string.IsNullOrWhiteSpace(clientIdRaw))
            throw new PluginMisconfigurationException("Client ID is missing in credentials.");

        if (!int.TryParse(clientIdRaw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var clientId))
            throw new PluginMisconfigurationException($"Client ID must be an integer. Got: '{clientIdRaw}'.");

        if (string.IsNullOrWhiteSpace(input.ProjectId))
            throw new PluginApplicationException("Order ID must be specified.");

        if (!int.TryParse(input.ProjectId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var projectId))
            throw new PluginApplicationException($"Order ID must be an integer. Got: '{input.ProjectId}'.");

        var body = new TranslateTextRequest
        {
            ClientId = clientId,
            ProjectId = projectId,
            ClientApplication = input.ClientApplication ?? "Blackbird",
            DocumentName = input.DocumentName ?? "Inline text",
            Domain = input.Domain,
            Provider = input.Provider,
            TranslationDirection = new TranslationDirection
            {
                SourceLanguage = input.SourceLanguage,
                TargetLanguage = input.TargetLanguage
            },
            OriginalText = new List<string> { input.Text }
        };

        var request = new RestRequest("/api/v1/Translation/Text", Method.Post);

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        var json = JsonSerializer.Serialize(body, jsonOptions);
        request.AddStringBody(json, DataFormat.Json);

        var apiResponse = await Client.ExecuteWithErrorHandling<TextTranslationApiResponse>(request);

        var first = apiResponse.TranslatedTexts?.FirstOrDefault();

        return new TranslateTextResponse
        {
            SourceLanguageCode = input.SourceLanguage,
            TargetLanguageCode = input.TargetLanguage,
            SourceText = first?.OriginalText ?? input.Text,
            TranslatedText = first?.TranslatedText ?? string.Empty
        };
    }

    [BlueprintActionDefinition(BlueprintAction.TranslateFile)]
    [Action("Translate", Description = "Translate a file using MT")]
    public async Task<FileTranslationResponse> Translate([ActionParameter] TranslateFileRequest input)
    {
        var strategy = input.FileTranslationStrategy?.ToLowerInvariant() ?? "blackbird";
        if (strategy == "propio")
        {
            return await TranslateWithPropioNative(input);
        }

        // default: blackbird
        try
        {
            using var stream = await fileManagement.DownloadAsync(input.File);
            var content = await Transformation.Parse(stream, input.File.Name);

            return await HandleInteroperableTransformation(content, input);
        }
        catch (Exception e) when (e.Message.Contains("not supported", StringComparison.OrdinalIgnoreCase))
        {
            throw new PluginMisconfigurationException(
                "The file format is not supported by the Blackbird interoperable strategy.");
        }
    }

    private async Task<FileTranslationResponse> HandleInteroperableTransformation(Transformation content, TranslateFileRequest input)
    {
        if (!string.IsNullOrWhiteSpace(input.SourceLanguage))
            content.SourceLanguage = input.SourceLanguage;

        if (!string.IsNullOrWhiteSpace(input.TargetLanguage))
            content.TargetLanguage = input.TargetLanguage;

        if (string.IsNullOrWhiteSpace(content.SourceLanguage) || string.IsNullOrWhiteSpace(content.TargetLanguage))
            throw new PluginMisconfigurationException("Source or target language not defined.");

        if (string.IsNullOrWhiteSpace(input.Domain))
            throw new PluginApplicationException("Domain must be specified.");

        var clientId = GetClientIdFromCreds(invocationContext);
        var projectId = ParseProjectId(input.ProjectId);

        static string RenderLine(List<LineElement>? line) =>
            line == null || line.Count == 0 ? string.Empty : string.Concat(line.Select(e => e.Render()));

        static List<LineElement> MakeLine(string text) =>
            new() { new LineElement { Value = text } };

        var overwriteExistingTargets = true;

        bool SegmentFilter(Segment s)
        {
            if (string.IsNullOrWhiteSpace(RenderLine(s.Source)))
                return false;

            var isInitial = s.State == null || s.State == SegmentState.Initial;
            if (!isInitial)
                return false;

            if (!overwriteExistingTargets)
            {
                var target = RenderLine(s.Target);
                if (!string.IsNullOrWhiteSpace(target))
                    return false;
            }

            return true;
        }

        var units = content.GetUnits()
            .Where(u => u?.Name != null)
            .ToList();

        if (!units.SelectMany(u => u.Segments).Any(SegmentFilter))
            return await BuildFileResponseByFormat(content, input);

        var processed = await units
            .Batch(batchSize: 50, segmentFilter: SegmentFilter)
            .Process<string>(async batch =>
            {
                var sourceTexts = batch.Select(x => RenderLine(x.Segment.Source)).ToList();

                var translatedTexts = await TranslateBatchViaTextEndpoint(
                    clientId, projectId, input,
                    content.SourceLanguage!, content.TargetLanguage!,
                    sourceTexts);

                if (translatedTexts.Count != sourceTexts.Count)
                {
                    translatedTexts = translatedTexts
                        .Take(sourceTexts.Count)
                        .Concat(Enumerable.Repeat(string.Empty, Math.Max(0, sourceTexts.Count - translatedTexts.Count)))
                        .ToList();
                }

                return translatedTexts;
            });

        foreach ((Unit Unit, IEnumerable<(Segment Segment, string Result)> Results) item in processed)
        {
            foreach ((Segment Segment, string Result) r in item.Results)
            {
                if (string.IsNullOrWhiteSpace(r.Result))
                    continue;

                r.Segment.Target = MakeLine(r.Result);
            }
        }

        return await BuildFileResponseByFormat(content, input);
    }

    private async Task<FileTranslationResponse> BuildFileResponseByFormat(Transformation content, TranslateFileRequest input)
    {
        if (input.OutputFileHandling?.Equals("original", StringComparison.OrdinalIgnoreCase) == true)
        {
            try
            {
                var targetContent = content.Target();
                var outFile = await fileManagement.UploadAsync(
                    targetContent.Serialize().ToStream(),
                    targetContent.OriginalMediaType ?? "application/octet-stream",
                    targetContent.OriginalName ?? input.File.Name);

                return new FileTranslationResponse { File = outFile };
            }
            catch
            {
                var xliffFallback = await fileManagement.UploadAsync(
                    content.Serialize().ToStream(),
                    MediaTypes.Xliff,
                    content.XliffFileName);

                return new FileTranslationResponse { File = xliffFallback };
            }
        }

        if (input.OutputFileHandling?.Equals("xliff1", StringComparison.OrdinalIgnoreCase) == true)
        {
            var xliff1String = Xliff1Serializer.Serialize(content);
            var file = await fileManagement.UploadAsync(
                xliff1String.ToStream(),
                MediaTypes.Xliff,
                content.XliffFileName);

            return new FileTranslationResponse { File = file };
        }

        var resultXliff = await fileManagement.UploadAsync(
            content.Serialize().ToStream(),
            MediaTypes.Xliff,
            content.XliffFileName);

        return new FileTranslationResponse { File = resultXliff };
    }

    private async Task<List<string>> TranslateBatchViaTextEndpoint(int clientId, int projectId, TranslateFileRequest input,
        string sourceLanguage, string targetLanguage, List<string> sourceTexts)
    {
        var body = new TranslateTextRequest
        {
            ClientId = clientId,
            ProjectId = projectId,
            ClientApplication = input.ClientApplication ?? "Blackbird",
            DocumentName = input.DocumentName ?? input.File.Name,
            Domain = input.Domain,
            Provider = input.Provider,
            TranslationDirection = new TranslationDirection
            {
                SourceLanguage = sourceLanguage,
                TargetLanguage = targetLanguage
            },
            OriginalText = sourceTexts
        };

        var request = new RestRequest("/api/v1/Translation/TextTranslation", Method.Post);

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        request.AddStringBody(JsonSerializer.Serialize(body, jsonOptions), DataFormat.Json);

        var apiResponse = await Client.ExecuteWithErrorHandling<TextTranslationApiResponse>(request);

        return apiResponse.TranslatedTexts?
                   .Select(x => x.TranslatedText ?? string.Empty)
                   .ToList()
               ?? new List<string>();
    }

    private async Task<FileTranslationResponse> TranslateWithPropioNative(TranslateFileRequest input)
    {
        var clientId = GetClientIdFromCreds(invocationContext);
        var projectId = ParseProjectId(input.ProjectId);

        if (string.IsNullOrWhiteSpace(input.SourceLanguage) || string.IsNullOrWhiteSpace(input.TargetLanguage))
            throw new PluginMisconfigurationException("Source or target language not defined.");

        if (string.IsNullOrWhiteSpace(input.Domain))
            throw new PluginApplicationException("Domain must be specified.");

        var originalName = input.File?.Name ?? "file";
        var safeMultipartName = SanitizeMultipartFileName(originalName);

        string documentId;

        using (var stream = await fileManagement.DownloadAsync(input.File))
        {
            var bytes = await ReadAllBytesAsync(stream);

            documentId = await UploadDocumentForTranslation(
                bytes,
                safeMultipartName,
                clientId,
                projectId,
                input);
        }

        if (string.IsNullOrWhiteSpace(documentId))
            throw new PluginApplicationException("No documentId returned after file upload.");

        var polls = 0;

        while (true)
        {
            await Task.Delay(2000);

            var status = await GetDocumentStatus(documentId);

            var st = status?.Status?.Trim();
            if (string.IsNullOrWhiteSpace(st))
                throw new PluginApplicationException("Document status response did not contain 'status'.");

            if (st.Equals("Queued", StringComparison.OrdinalIgnoreCase))
                continue;

            if (st.Equals("Completed", StringComparison.OrdinalIgnoreCase))
                break;

            var details = string.Join(" | ",
                new[] { status?.Error, status?.TranslationDetails }
                    .Where(x => !string.IsNullOrWhiteSpace(x)));

            throw new PluginApplicationException($"Document translation finished with status '{st}'. {details}");
        }

        var link = await GetTranslatedFileLink(documentId);
        if (string.IsNullOrWhiteSpace(link?.FileUrl))
            throw new PluginApplicationException("Translated file URL was not returned.");

        var translatedBytes = await DownloadFromPublicUrl(link.FileUrl);

        var outName = !string.IsNullOrWhiteSpace(link.FileName) ? link.FileName : originalName;

        var outFile = await fileManagement.UploadAsync(
            new MemoryStream(translatedBytes),
            "application/octet-stream",
            outName);

        return new FileTranslationResponse { File = outFile };
    }

    private async Task<string> UploadDocumentForTranslation(
        byte[] fileBytes,
        string fileName,
        int clientId,
        int projectId,
        TranslateFileRequest input)
    {
        var model = new PropioDocumentTranslationModel
        {
            JobId = ResolveJobId(input.JobId),
            ClientId = clientId,
            ProjectId = projectId,
            ClientApplication = input.ClientApplication ?? "Blackbird PropioOne",
            Domain = input.Domain,
            Provider = input.Provider,
            TranslationDirection = new PropioTranslationDirection
            {
                SourceLanguage = input.SourceLanguage,
                TargetLanguage = input.TargetLanguage
            }
        };

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        var request = new RestRequest("/api/v1/Translation/Document", Method.Post)
        {
            AlwaysMultipartFormData = true
        };

        request.AddFile("DocumentToTranslate", fileBytes, fileName);
        request.AddParameter("TranslationModel", JsonSerializer.Serialize(model, jsonOptions));

        var resp = await Client.ExecuteWithErrorHandling<string>(request);
        return resp?.Trim().Trim('"') ?? string.Empty;
    }

    private async Task<PropioDocumentStatusResponse> GetDocumentStatus(string documentId)
    {
        var request = new RestRequest($"/api/v1/Translation/Document/{documentId}", Method.Get);
        return await Client.ExecuteWithErrorHandling<PropioDocumentStatusResponse>(request);
    }

    private async Task<PropioTranslatedFileLinkResponse> GetTranslatedFileLink(string documentId)
    {
        var request = new RestRequest($"/api/v1/Translation/Document/{documentId}/Translated", Method.Get);
        return await Client.ExecuteWithErrorHandling<PropioTranslatedFileLinkResponse>(request);
    }

    private static async Task<byte[]> DownloadFromPublicUrl(string url)
    {
        var client = new RestClient(url);
        var request = new RestRequest("", Method.Get);

        var response = await client.ExecuteAsync(request);
        if (!response.IsSuccessful || response.RawBytes == null)
            throw new PluginApplicationException($"Failed to download translated file. HTTP {(int)response.StatusCode} {response.StatusDescription}");

        return response.RawBytes;
    }

    private static int ResolveJobId(int? inputJobId)
    {
        if (inputJobId.HasValue)
        {
            if (inputJobId.Value <= 0)
                throw new PluginMisconfigurationException("Job ID must be a positive integer.");
            return inputJobId.Value;
        }

        return RandomNumberGenerator.GetInt32(1, int.MaxValue);
    }
    private static async Task<byte[]> ReadAllBytesAsync(Stream s)
    {
        using var ms = new MemoryStream();
        await s.CopyToAsync(ms);
        return ms.ToArray();
    }

    private static string SanitizeMultipartFileName(string fileName)
    {
        fileName = Path.GetFileName(fileName);

        var ext = Path.GetExtension(fileName);
        var baseName = Path.GetFileNameWithoutExtension(fileName);

        baseName = new string(baseName.Where(c => !char.IsControl(c)).ToArray());
        baseName = baseName.Replace("\"", "'");
        baseName = baseName.Replace("\\", "_");

        foreach (var ch in Path.GetInvalidFileNameChars())
            baseName = baseName.Replace(ch, '_');

        baseName = baseName.Trim();
        if (string.IsNullOrWhiteSpace(baseName))
            baseName = "file";

        return baseName + ext;
    }

    private static int GetClientIdFromCreds(InvocationContext ctx)
    {
        string? clientIdRaw = ctx.AuthenticationCredentialsProviders
            .FirstOrDefault(x => x.KeyName == CredsNames.ClientId)?.Value;

        if (string.IsNullOrWhiteSpace(clientIdRaw))
            throw new PluginMisconfigurationException("Client ID is missing in credentials.");

        if (!int.TryParse(clientIdRaw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var clientId))
            throw new PluginMisconfigurationException($"Client ID must be an integer. Got: '{clientIdRaw}'.");

        return clientId;
    }

    private static int ParseProjectId(string? projectIdRaw)
    {
        if (string.IsNullOrWhiteSpace(projectIdRaw))
            throw new PluginApplicationException("Order ID must be specified.");

        if (!int.TryParse(projectIdRaw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var projectId))
            throw new PluginApplicationException($"Order ID must be an integer. Got: '{projectIdRaw}'.");

        return projectId;
    }

    [BlueprintActionDefinition(BlueprintAction.EditFile)]
    [Action("Edit", Description = "Edit a translation using Propio APE. Assumes translated content was produced earlier.")]
    public async Task<EditFileResponse> Edit([ActionParameter] EditFileRequest input)
    {
        await using var stream = await fileManagement.DownloadAsync(input.File);
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);

        var contentString = System.Text.Encoding.UTF8.GetString(ms.ToArray());

        var content = Transformation.Parse(contentString, input.File.Name)
            ?? throw new PluginApplicationException(
                "Failed to parse bilingual file. Please send the file to the team for inspection.");

        var srcLang = input.SourceLanguage ?? content.SourceLanguage;

        var trgLang = input.TargetLanguage ?? content.TargetLanguage;

        var items = GetSegmentsToProcess(content)
            .Select(x => new SegmentWorkItem(
                x.unit,
                x.segment,
                x.segment.GetSource(),
                x.segment.GetTarget()))
            .Where(x => !string.IsNullOrWhiteSpace(x.Source) && !string.IsNullOrWhiteSpace(x.Target))
            .ToList();

        int reviewed = 0;
        int updated = 0;

        if (items.Count == 0)
        {
            var passthroughStream = BuildOutputStream(content, contentString, input.OutputFileHandling);
            var passthroughUploaded = await fileManagement.UploadAsync(
                passthroughStream, input.File.ContentType, input.File.Name);

            return new EditFileResponse
            {
                File = passthroughUploaded,
                TotalSegmentsReviewed = 0,
                TotalSegmentsUpdated = 0
            };
        }

        const int batchSize = 50;
        for (int i = 0; i < items.Count; i += batchSize)
        {
            var batch = items.Skip(i).Take(batchSize).ToList();

            var request = new RestRequest("/api/v1/PostEditing/GetApeSegments", Method.Post);
            request.AddJsonBody(new GetApeSegmentsRequest
            {
                OriginalText = batch.Select(b => b.Source).ToList(),
                TranslatedText = batch.Select(b => b.Target).ToList(),
                Domain = input.Domain ?? "General Vocabulary",
                SourceLanguage = srcLang,
                TargetLanguage = trgLang,
                SourceFile = "",
                TargetFile = ""
            });

            var resp = await Client.ExecuteWithErrorHandling<GetApeSegmentsResponse>(request);

            var edits = FlattenOrdered(resp);

            var countToApply = Math.Min(batch.Count, edits.Count);

            for (int j = 0; j < batch.Count; j++)
            {
                var item = batch[j];
                var editedText = j < countToApply ? edits[j].Ape : null;

                reviewed++;

                if (!string.IsNullOrWhiteSpace(editedText) &&
                    !string.Equals(editedText, item.Target, StringComparison.Ordinal))
                {
                    item.Segment.SetTarget(editedText);
                    updated++;

                    item.Unit.Notes.Add(new Note("Edited by Proprio APE") { Reference = item.Segment.Id });
                }
                else
                {
                    item.Unit.Notes.Add(new Note("Reviewed by Proprio APE (no changes)") { Reference = item.Segment.Id });
                }

                item.Segment.State = SegmentState.Reviewed;
                item.Unit.Provenance.Review.Tool = "Proprio APE";
            }
        }

        Stream outputStream = BuildOutputStream(content, contentString, input.OutputFileHandling);

        var uploaded = await fileManagement.UploadAsync(outputStream, input.File.ContentType, input.File.Name);

        return new EditFileResponse
        {
            File = uploaded,
            TotalSegmentsReviewed = reviewed,
            TotalSegmentsUpdated = updated
        };
    }

    [BlueprintActionDefinition(BlueprintAction.EditText)]
    [Action("Edit text", Description = "Post-edit already translated text")]
    public async Task<EditTextResponse> EditText([ActionParameter] EditTextRequest input)
    {
        if (string.IsNullOrWhiteSpace(input.SourceLanguage))
            throw new PluginMisconfigurationException("Source language is required for this action (e.g., en-US).");

        if (string.IsNullOrWhiteSpace(input.TargetLanguage))
            throw new PluginMisconfigurationException("Target language is required for this action (e.g., de-DE).");

        var request = new RestRequest("/api/v1/PostEditing/GetApeSegments", Method.Post);
        request.AddJsonBody(new GetApeSegmentsRequest
        {
            OriginalText = new() { input.SourceText },
            TranslatedText = new() { input.TargetText },
            Domain = input.Domain ?? "General Vocabulary",
            SourceLanguage = input.SourceLanguage,
            TargetLanguage = input.TargetLanguage,
            SourceFile = "",
            TargetFile = ""
        });

        var resp = await Client.ExecuteWithErrorHandling<GetApeSegmentsResponse>(request);
        var edited = FlattenOrdered(resp).FirstOrDefault()?.Ape;

        return new EditTextResponse
        {
            EditedText = string.IsNullOrWhiteSpace(edited) ? input.TargetText : edited
        };
    }

    private static IEnumerable<(Unit unit, Segment segment)> GetSegmentsToProcess(Transformation transformation)
    {
        foreach (var unit in transformation.GetUnits())
        {
            if (unit.IsInitial || unit.State == SegmentState.Final)
                continue;

            foreach (var segment in unit.Segments)
            {
                if (segment.IsIgnorbale || segment.State == SegmentState.Final)
                    continue;

                yield return (unit, segment);
            }
        }
    }

    private sealed record SegmentWorkItem(Unit Unit, Segment Segment, string Source, string Target);

    public List<ApeSegmentDto> FlattenOrdered(GetApeSegmentsResponse? response)
    {
        if (response?.Ape == null || response.Ape.Count == 0)
            return [];


        var indexed = new List<(int Index, ApeSegmentDto Segment)>();

        foreach (var dict in response.Ape)
        {
            if (dict == null) continue;

            foreach (var kv in dict)
            {
                if (!int.TryParse(kv.Key, out var index))
                    continue;

                if (kv.Value == null) continue;

                indexed.Add((index, kv.Value));
            }
        }

        return indexed
            .OrderBy(x => x.Index)
            .Select(x => x.Segment)
            .ToList();
    }

    private static Stream BuildOutputStream(Transformation content, string originalFileString, string outputHandling)
    {
        outputHandling ??= string.Empty;

        if (string.Equals(outputHandling, "original", StringComparison.OrdinalIgnoreCase))
        {
            if (Xliff2Serializer.IsXliff2(originalFileString))
            {
                return Xliff2Serializer.Serialize(content).ToStream();
            }

            try
            {
                var target = content.Target();
                return target.Serialize().ToStream();
            }
            catch
            {
                return content.Serialize().ToStream();
            }
        }

        if (string.Equals(outputHandling, "xliff", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(outputHandling, "xliff2", StringComparison.OrdinalIgnoreCase))
        {
            return Xliff2Serializer.Serialize(content).ToStream();
        }

        return content.Serialize().ToStream();
    }


    [BlueprintActionDefinition(BlueprintAction.ReviewText)]
    [Action("Review text", Description = "Reviews translation quality using Basic quality estimation (source/target text are sent as .txt files).")]
    public async Task<ReviewTextResponse> ReviewText([ActionParameter] ReviewTextRequest input)
    {
        if (string.IsNullOrWhiteSpace(input.SourceText))
            throw new PluginMisconfigurationException("Source text is required.");

        if (string.IsNullOrWhiteSpace(input.TargetText))
            throw new PluginMisconfigurationException("Target text is required.");

        if (string.IsNullOrWhiteSpace(input.SourceLanguage))
            throw new PluginMisconfigurationException("Source language is required.");

        if (string.IsNullOrWhiteSpace(input.TargetLanguage))
            throw new PluginMisconfigurationException("Target language is required.");

        var domain = string.IsNullOrWhiteSpace(input.Domain) ? "General Vocabulary" : input.Domain;

        var sourceBytes = Encoding.UTF8.GetBytes(input.SourceText);
        var targetBytes = Encoding.UTF8.GetBytes(input.TargetText);

        var request = new RestRequest("/api/v1/QualityEstimation/BasicQEScore", Method.Post)
        {
            AlwaysMultipartFormData = true
        };

        request.AddFile("targetfile", targetBytes, "target.txt");
        request.AddFile("sourcefile", sourceBytes, "source.txt");

        request.AddParameter("domain", domain);
        request.AddParameter("sourceLanguage", input.SourceLanguage);
        request.AddParameter("targetLanguage", input.TargetLanguage);

        var api = await Client.ExecuteWithErrorHandling<BasicQeApiResponse>(request);

        var avg = api.BasicQEscoreAVG ?? 0.0;
        var normalized = avg > 1.0 ? avg / 100.0 : avg;

        return new ReviewTextResponse
        {
            Score = (float)normalized
        };
    }

    [BlueprintActionDefinition(BlueprintAction.ReviewFile)]
    [Action("Review", Description = "Reviews a translated file using Basic QE. Supports blackbird (segment-based) and propio (file-to-file) strategies.")]
    public async Task<QualityEstimationResponse> Review([ActionParameter] QualityEstimationRequest input)
    {
        if (input.File == null)
            throw new PluginMisconfigurationException("Source file is required.");

        var strategy = (input.ReviewStrategy ?? "blackbird").Trim().ToLowerInvariant();
        if (strategy == "propio")
            return await ReviewWithPropio(input);

        return await ReviewWithBlackbird(input);
    }

    private async Task<QualityEstimationResponse> ReviewWithBlackbird(QualityEstimationRequest input)
    {
        var threshold = input.Threshold ?? 0.8;
        if (threshold < 0 || threshold > 1)
            throw new PluginMisconfigurationException("Threshold must be in range 0..1.");

        using var stream = await fileManagement.DownloadAsync(input.File!);
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        ms.Position = 0;

        Transformation content;
        try
        {
            content = await Transformation.Parse(ms, input.File!.Name);
        }
        catch
        {
            var contentStringFallback = Encoding.UTF8.GetString(ms.ToArray());
            content = Transformation.Parse(contentStringFallback, input.File!.Name)
                      ?? throw new PluginApplicationException("Could not parse this file as XLIFF/Transformation.");
        }

        content.SourceLanguage ??= input.SourceLanguage;
        content.TargetLanguage ??= input.TargetLanguage;

        if (string.IsNullOrWhiteSpace(content.SourceLanguage))
            throw new PluginMisconfigurationException("Source language is not defined. Provide SourceLanguage.");
        if (string.IsNullOrWhiteSpace(content.TargetLanguage))
            throw new PluginMisconfigurationException("Target language is not defined. Provide TargetLanguage.");

        var srcLang = content.SourceLanguage!;
        var trgLang = content.TargetLanguage!;
        var domain = string.IsNullOrWhiteSpace(input.Domain) ? "General Vocabulary" : input.Domain;

        int processedSegmentsCount = 0;
        int finalizedSegmentsCount = 0;
        int underThresholdCount = 0;
        double totalScore = 0.0;

        static string RenderLine(List<LineElement>? line) =>
            line == null || line.Count == 0 ? string.Empty : string.Concat(line.Select(e => e.Render()));

        bool SegmentFilter(Segment s)
        {
            if (s == null) return false;
            if (s.IsIgnorbale) return false;
            if (s.State == SegmentState.Final) return false;

            return true;
        }

        async Task<double> EstimateSegmentScore(string source, string target)
        {
            var request = new RestRequest("/api/v1/QualityEstimation/BasicQEScore", Method.Post)
            {
                AlwaysMultipartFormData = true
            };

            var sourceBytes = Encoding.UTF8.GetBytes(source);
            var targetBytes = Encoding.UTF8.GetBytes(target);

            request.AddFile("sourcefile", sourceBytes, "source.txt");
            request.AddFile("targetfile", targetBytes, "target.txt");

            request.AddParameter("domain", domain);
            request.AddParameter("sourceLanguage", srcLang);
            request.AddParameter("targetLanguage", trgLang);

            var api = await Client.ExecuteWithErrorHandling<BasicQeApiResponse>(request);
            var avg = api.BasicQEscoreAVG ?? 0.0;

            return avg > 1.0 ? avg / 100.0 : avg;
        }

        var units = content.GetUnits().ToList();

        foreach (var unitChunk in units.Chunk(10))
        {
            foreach (var unit in unitChunk)
            {
                double unitScoreSum = 0.0;
                int unitCount = 0;

                foreach (var segment in unit.Segments.Where(SegmentFilter))
                {
                    var src = RenderLine(segment.Source);
                    var trg = RenderLine(segment.Target);

                    var score = await EstimateSegmentScore(src, trg);

                    processedSegmentsCount++;
                    totalScore += score;

                    unitScoreSum += score;
                    unitCount++;

                    if (score >= threshold)
                    {
                        segment.State = SegmentState.Final;
                        finalizedSegmentsCount++;
                    }
                    else
                    {
                        underThresholdCount++;
                    }
                }

                if (unitCount > 0)
                {
                    unit.Quality.ProfileReference = "https://tgw.propio-ls.com/api/v1/QualityEstimation/BasicQEScore";
                    unit.Quality.ScoreThreshold = threshold;
                    unit.Quality.Score = (float)(unitScoreSum / unitCount);
                }
            }
        }

        Stream streamResult;
        var contentString = Encoding.UTF8.GetString(ms.ToArray());

        if (input.OutputFileHandling?.Equals("original", StringComparison.OrdinalIgnoreCase) == true)
        {
            if (Xliff1Serializer.IsXliff1(contentString))
            {
                var xliff1String = Xliff1Serializer.Serialize(content);
                streamResult = xliff1String.ToStream();
            }
            else
            {
                var targetContent = content.Target();
                streamResult = targetContent.Serialize().ToStream();
            }
        }
        else
        {
            streamResult = content.Serialize().ToStream();
        }

        var finalFile = await fileManagement.UploadAsync(
            streamResult,
            input.File!.ContentType ?? MediaTypes.Xliff,
            input.File!.Name);

        var avgMetric = processedSegmentsCount > 0 ? (float)(totalScore / processedSegmentsCount) : 0f;
        var pctUnder = processedSegmentsCount > 0 ? (float)underThresholdCount / processedSegmentsCount : 0f;

        return new QualityEstimationResponse
        {
            File = finalFile,
            TotalSegmentsProcessed = processedSegmentsCount,
            TotalSegmentsFinalized = finalizedSegmentsCount,
            TotalSegmentsUnderThreshhold = underThresholdCount,
            AverageMetric = avgMetric,
            PercentageSegmentsUnderThreshhold = pctUnder
        };
    }

    private async Task<QualityEstimationResponse> ReviewWithPropio(QualityEstimationRequest input)
    {
        if (input.File == null)
            throw new PluginMisconfigurationException("Source file is required.");

        if (input.TargetFile == null)
            throw new PluginMisconfigurationException("For 'propio' strategy you must provide TargetFile.");

        if (string.IsNullOrWhiteSpace(input.SourceLanguage) || string.IsNullOrWhiteSpace(input.TargetLanguage))
            throw new PluginMisconfigurationException("Source and target language must be specified.");

        var domain = string.IsNullOrWhiteSpace(input.Domain) ? "General Vocabulary" : input.Domain;

        byte[] sourceBytes;
        byte[] targetBytes;

        using (var s = await fileManagement.DownloadAsync(input.File))
            sourceBytes = await ReadAllBytesAsync(s);

        using (var s = await fileManagement.DownloadAsync(input.TargetFile))
            targetBytes = await ReadAllBytesAsync(s);

        var request = new RestRequest("/api/v1/QualityEstimation/BasicQEScore", Method.Post)
        {
            AlwaysMultipartFormData = true
        };

        request.AddFile("sourcefile", sourceBytes, SanitizeMultipartFileName(input.File.Name));
        request.AddFile("targetfile", targetBytes, SanitizeMultipartFileName(input.TargetFile.Name));

        request.AddParameter("domain", domain);
        request.AddParameter("sourceLanguage", input.SourceLanguage);
        request.AddParameter("targetLanguage", input.TargetLanguage);

        var api = await Client.ExecuteWithErrorHandling<BasicQeApiResponse>(request);

        var avg = api.BasicQEscoreAVG > 1.0 ? api.BasicQEscoreAVG / 100.0 : api.BasicQEscoreAVG;

        return new QualityEstimationResponse
        {
            File = input.TargetFile,
            TotalSegmentsProcessed = 0,
            TotalSegmentsFinalized = 0,
            TotalSegmentsUnderThreshhold = 0,
            AverageMetric = (float)avg,
            PercentageSegmentsUnderThreshhold = 0f
        };
    }
}

