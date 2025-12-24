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

            var request = new RestRequest("/api/v1/Translation/TextTranslation", Method.Post);

            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            var json = JsonSerializer.Serialize(body, jsonOptions);
            request.AddStringBody(json, DataFormat.Json);

            var apiResponse = await Client.ExecuteWithErrorHandling<TextTranslationApiResponse>(request);

            return new TranslateTextResponse
            {
                SourceLanguageCode = apiResponse.TranslationDirection?.SourceLanguage ?? input.SourceLanguage,
                TargetLanguageCode = apiResponse.TranslationDirection?.TargetLanguage ?? input.TargetLanguage,
                SourceText = apiResponse.OriginalText?.FirstOrDefault() ?? input.Text,
                TranslatedText = apiResponse.TranslatedText?.FirstOrDefault() ?? string.Empty
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
            content.SourceLanguage ??= input.SourceLanguage;
            content.TargetLanguage ??= input.TargetLanguage;

            if (string.IsNullOrWhiteSpace(content.SourceLanguage) || string.IsNullOrWhiteSpace(content.TargetLanguage))
                throw new PluginMisconfigurationException("Source or target language not defined.");

            if (string.IsNullOrWhiteSpace(input.Domain))
                throw new PluginApplicationException("Domain must be specified.");

            var clientId = GetClientIdFromCreds(invocationContext);
            var projectId = ParseProjectId(input.ProjectId);

            var units = content.GetUnits()
                .Where(u => u.Name != null)
                .ToList();

            units = units
                .Where(u => !string.IsNullOrWhiteSpace(ExtractSourceText(u)))
                .ToList();

            if (units.Count == 0)
            {
                if (input.OutputFileHandling?.Equals("original", StringComparison.OrdinalIgnoreCase) == true)
                {
                    using var originalStream = await fileManagement.DownloadAsync(input.File);
                    var outFile = await fileManagement.UploadAsync(
                        originalStream,
                        input.File.ContentType ?? "application/octet-stream",
                        input.File.Name);

                    return new FileTranslationResponse { File = outFile };
                }

                var xliffFile = await fileManagement.UploadAsync(
                    content.Serialize().ToStream(),
                    MediaTypes.Xliff,
                    content.XliffFileName);

                return new FileTranslationResponse { File = xliffFile };
            }

            foreach (var batch in units.Chunk(50))
            {
                var sourceTexts = batch.Select(u => ExtractSourceText(u)).ToList();

                var translatedTexts = await TranslateBatchViaTextEndpoint(
                    clientId, projectId, input,
                    content.SourceLanguage!, content.TargetLanguage!,
                    sourceTexts);

                var count = Math.Min(batch.Length, translatedTexts.Count);

                for (int i = 0; i < count; i++)
                {
                    var translated = translatedTexts[i];
                    if (string.IsNullOrWhiteSpace(translated))
                        continue;

                    ApplyTargetText(batch[i], translated);
                }
            }

            if (input.OutputFileHandling?.Equals("original", StringComparison.OrdinalIgnoreCase) == true)
            {
                var targetContent = content.Target();
                var outFile = await fileManagement.UploadAsync(
                    targetContent.Serialize().ToStream(),
                    targetContent.OriginalMediaType ?? "application/octet-stream",
                    targetContent.OriginalName ?? input.File.Name);

                return new FileTranslationResponse { File = outFile };
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

            var json = JsonSerializer.Serialize(body, jsonOptions);
            request.AddStringBody(json, DataFormat.Json);

            var apiResponse = await Client.ExecuteWithErrorHandling<TextTranslationApiResponse>(request);

            return apiResponse.TranslatedText ?? new List<string>();
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

        private static string ExtractSourceText(Unit unit)
        {
            TextUnit source = unit.GetSource();
            return source.GetNormalizedText();
        }

        private static void ApplyTargetText(Unit unit, string translated)
        {
            TextUnit target = unit.GetTarget();
            target.SetCodedText(translated);
        }
    }
}
