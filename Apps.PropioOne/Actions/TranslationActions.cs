using Apps.PropioOne.Constants;
using Apps.PropioOne.Models.Translate;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Blueprints;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using Blackbird.Filters.Constants;
using Blackbird.Filters.Content;
using Blackbird.Filters.Enums;
using Blackbird.Filters.Extensions;
using Blackbird.Filters.Transformations;
using Blackbird.Filters.Xliff.Xliff1;
using RestSharp;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Apps.PropioOne.Actions
{
    [ActionList("Translation")]
    public class TranslationActions(InvocationContext invocationContext, IFileManagementClient fileManagement) : PropioOneInvocable(invocationContext)
    {
        [BlueprintActionDefinition(BlueprintAction.TranslateText)]
        [Action("Translate text", Description = "Localize the text provided.")]
        public async Task<TranslateTextResponse> TranslateText([ActionParameter] TranslateTextInput input)
        {
            string? clientIdRaw = invocationContext.AuthenticationCredentialsProviders.FirstOrDefault(x => x.KeyName == CredsNames.ClientId)?.Value;

            if (string.IsNullOrWhiteSpace(clientIdRaw))
                throw new PluginMisconfigurationException("Client ID is missing in credentials.");

            if (!int.TryParse(clientIdRaw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var clientId))
                throw new PluginMisconfigurationException($"Client ID must be an integer. Got: '{clientIdRaw}'.");

            if (string.IsNullOrWhiteSpace(input.ProjectId))
                throw new PluginApplicationException("ProjectId must be specified.");

            if (!int.TryParse(input.ProjectId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var projectId))
                throw new PluginApplicationException($"ProjectId must be an integer. Got: '{input.ProjectId}'.");

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
        [Action("Translate", Description = "Translate a file")]
        public async Task<FileTranslationResponse> Translate([ActionParameter] TranslateFileRequest input)
        {
            //var strategy = input.FileTranslationStrategy?.ToLowerInvariant() ?? "blackbird";
            //if (strategy == "propio")
            //{
            //    return await TranslateWithPropioNative(input);
            //}
            // default: "blackbird"

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

        private async Task<List<string>> TranslateBatchViaTextEndpoint(int clientId,int projectId,TranslateFileRequest input,
            string sourceLanguage,string targetLanguage,List<string> sourceTexts)
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


        //helpers
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
                throw new PluginApplicationException("ProjectId must be specified.");

            if (!int.TryParse(projectIdRaw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var projectId))
                throw new PluginApplicationException($"ProjectId must be an integer. Got: '{projectIdRaw}'.");

            return projectId;
        }
    }
}
