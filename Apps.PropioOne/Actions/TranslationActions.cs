using Apps.PropioOne.Models.Translate;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Blueprints;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using RestSharp;

namespace Apps.PropioOne.Actions
{
    [ActionList("Translation")]
    public class TranslationActions(InvocationContext invocationContext, IFileManagementClient fileManagement) : PropioOneInvocable(invocationContext)
    {
        [BlueprintActionDefinition(BlueprintAction.TranslateText)]
        [Action("Translate text", Description = "Localize the text provided.")]
        public async Task<TranslateTextResponse> TranslateText([ActionParameter] TranslateTextInput input)
        {
            var body = new TranslateTextRequest
            {
                ClientId = input.ClientId,
                ProjectId = input.ProjectId,
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
            request.AddJsonBody(body);

            var apiResponse = await Client
                .ExecuteWithErrorHandling<TextTranslationApiResponse>(request);

            var translated = apiResponse.TranslatedText?.FirstOrDefault() ?? string.Empty;
            var original = apiResponse.OriginalText?.FirstOrDefault() ?? input.Text;

            return new TranslateTextResponse
            {
                SourceLanguageCode = apiResponse.TranslationDirection?.SourceLanguage ?? input.SourceLanguage,
                TargetLanguageCode = apiResponse.TranslationDirection?.TargetLanguage ?? input.TargetLanguage,
                SourceText = original,
                TranslatedText = translated
            };
        }
    }
}
