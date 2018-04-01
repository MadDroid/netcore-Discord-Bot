using Newtonsoft.Json;
namespace Bot.Models
{
    public class Stream
    {
        [JsonProperty("stramName")]
        public string Name { get; set; }

        [JsonProperty("streamChannel")]
        public string Channel { get; set; }

        [JsonProperty("streamLanguage")]
        public string Language { get; set; }

        [JsonProperty("streamPlatform")]
        public string Platform { get; set; }
    }
}