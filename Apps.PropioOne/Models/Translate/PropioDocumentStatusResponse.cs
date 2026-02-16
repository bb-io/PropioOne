using System.Text.Json.Serialization;

namespace Apps.PropioOne.Models.Translate;

public class PropioDocumentStatusResponse
{
    [JsonPropertyName("documentId")]
    public string? DocumentId { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }

    [JsonPropertyName("translationDetails")]
    public string? TranslationDetails { get; set; }

    [JsonPropertyName("fileName")]
    public string? FileName { get; set; }
}