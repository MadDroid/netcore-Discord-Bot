using Newtonsoft.Json;
namespace Bot.Models
{
    public class Series
    {
        [JsonProperty("scoreA")]
        public int ScoreA { get; set; }

        [JsonProperty("scoreB")]
        public int ScoreB { get; set; }
    }
}