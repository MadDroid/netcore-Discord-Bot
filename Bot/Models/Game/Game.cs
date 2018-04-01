using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Bot.Models
{
    public class Game
    {
        [JsonProperty("teamA")]
        public string TeamA { get; set; }

        [JsonProperty("teamACountry")]
        public string TeamACountry { get; set; }

        [JsonProperty("teamAId")]
        public int TeamAId { get; set; }

        [JsonProperty("teamB")]
        public string TeamB { get; set; }

        [JsonProperty("teamBCountry")]
        public string TeamBCountry { get; set; }

        [JsonProperty("teamBId")]
        public int TeamBId { get; set; }

        [JsonProperty("tournament")]
        public dynamic Tournament { get; set; }

        [JsonProperty("tournamentId")]
        public int TournamentId { get; set; }

        [JsonProperty("matchDate")]
        public long UnixMatchDate { get; set; }

        [JsonIgnore]
        private DateTime matchDate;

        [JsonIgnore]
        public DateTime MatchDate
        {
            get
            {
                if (matchDate == DateTime.MinValue)
                    return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(UnixMatchDate).ToLocalTime();
                return matchDate;
            }
            set
            {
                matchDate = value;
            }
        }

        [JsonProperty("matchId")]
        public int MatchId { get; set; }

        [JsonProperty("isFinished")]
        public int IsFinished { get; set; }

        [JsonProperty("isOver")]
        public int IsOver { get; set; }

        [JsonProperty("isTBA")]
        public int IsTBA { get; set; }

        [JsonProperty("isStreamed")]
        public int IsStreamed { get; set; }

        [JsonProperty("isFeatured")]
        public int IsFeatured { get; set; }

        [JsonProperty("coverage")]
        public Coverage Coverage { get; set; }

        [JsonProperty("stream")]
        public Stream Stream { get; set; }

        [JsonProperty("series")]
        public Series Series { get; set; }

        [JsonProperty("scores")]
        public List<Score> Scores { get; set; }

        public override string ToString()
        {
            return TeamA + " x " + TeamB;
        }
    }
}
