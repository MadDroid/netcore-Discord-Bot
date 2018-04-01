using Newtonsoft.Json;
namespace Bot.Models
{
    public class Score
    {
        [JsonProperty("scoreA")]
        public int ScoreA { get; set; }

        [JsonProperty("scoreB")]
        public int ScoreB { get; set; }

        [JsonProperty("map")]
        public string Map { get; set; }

        [JsonProperty("mapOrder")]
        public int MapOrder { get; set; }
    }
}