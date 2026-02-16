using System.Text.Json.Serialization;

namespace Apps.PropioOne.Models.Translate;

public class PropioTranslatedFileLinkResponse
{
    [JsonPropertyName("fileName")]
    public string? FileName { get; set; }

    [JsonPropertyName("fileURL")]
    public string? FileUrl { get; set; }

    [JsonPropertyName("urlExpiration")]
    public string? UrlExpiration { get; set; }
}

