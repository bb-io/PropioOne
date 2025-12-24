using Newtonsoft.Json;

namespace Apps.PropioOne.Models.Translate
{
    public class TranslateTextDto
    {
        [JsonProperty("translatedText")]
        public string TranslatedText { get; set; } = default!;
    }
}
