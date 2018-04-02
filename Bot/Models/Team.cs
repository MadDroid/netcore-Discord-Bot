using Newtonsoft.Json;
using System.Linq;

namespace Bot.Models
{
    public class Team
    {
        // TODO: Add Aliases

        [JsonProperty("teamId")]
        public int Id { get; set; }

        [JsonProperty("teamName")]
        public string Name { get; set; }

        [JsonProperty("teamCountry")]
        public string Country { get; set; }

        [JsonProperty("teamLogo")]
        public string Logo { get; set; }
    }
}
