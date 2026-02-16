using System.Text.Json.Serialization;

namespace Apps.PropioOne.Models.Translate;

public class PropioDocumentTranslationModel
{
    [JsonPropertyName("jobId")]
    public int? JobId { get; set; }

    [JsonPropertyName("clientId")]
    public int ClientId { get; set; }

    [JsonPropertyName("projectId")]
    public int ProjectId { get; set; }

    [JsonPropertyName("clientApplication")]
    public string? ClientApplication { get; set; }

    [JsonPropertyName("documentName")]
    public string? DocumentName { get; set; }

    [JsonPropertyName("translationDirection")]
    public PropioTranslationDirection TranslationDirection { get; set; } = new();

    [JsonPropertyName("domain")]
    public string? Domain { get; set; }

    [JsonPropertyName("provider")]
    public string? Provider { get; set; }
}

public class PropioTranslationDirection
{
    [JsonPropertyName("sourceLanguage")]
    public string? SourceLanguage { get; set; }

    [JsonPropertyName("targetLanguage")]
    public string? TargetLanguage { get; set; }
}
