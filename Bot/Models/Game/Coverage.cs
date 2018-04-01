using Newtonsoft.Json;
namespace Bot.Models
{
    public class Coverage
    {
        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }
}