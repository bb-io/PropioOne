using Newtonsoft.Json;

namespace Apps.PropioOne.Models
{
    public class LanguageDto
    {
        [JsonProperty("languageCode")]
        public string Code { get; set; } = default!;

        [JsonProperty("languageName")]
        public string Name { get; set; } = default!;
    }
}
