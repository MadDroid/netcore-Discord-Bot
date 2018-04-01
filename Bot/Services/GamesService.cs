using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using Bot.Helpers;
using Bot.Models;
using System.Threading;

namespace Bot.Services
{
    public class GamesService
    {
        #region Api
        const string BaseApi = "https://api.draft5.gg/api/v2/";
        const string futureMatch = "matches?filter[status]=0";
        #endregion

        #region Events
        public event EventHandler<GamesEventArgs> GotGames;
        private void OnGotGames(List<Game> games) => GotGames?.Invoke(this, new GamesEventArgs { Games = games });
        #endregion

        #region Private Fields
        private List<Game> games;
        private HashSet<string> teams;
        private Timer timer;
        private bool firstRun = true;
        #endregion

        #region Public Properties
        public List<Game> Games
        {
            get
            {
                if (games == null)
                    FetchGames().GetAwaiter().GetResult();
                return games;
            }
        }

        public List<string> Teams => teams.OrderBy(t => t).ToList();
        #endregion

        #region Constructor
        public GamesService()
        {
            if (!Directory.Exists(BotConfig.DataFolder))
            {
                Directory.CreateDirectory(BotConfig.DataFolder);
                LoggindService.Log("Diretório de dados criado", GetType(), Discord.LogSeverity.Info);
            }

            if (!File.Exists(BotConfig.GamesFile))
            {
                File.Create(BotConfig.GamesFile).Dispose();
                LoggindService.Log("Arquivo de jogos criado", GetType(), Discord.LogSeverity.Info);
            }

            if (!File.Exists(BotConfig.TeamsFile))
            {
                File.Create(BotConfig.TeamsFile).Dispose();
                LoggindService.Log("Arquivo de times criado", GetType(), Discord.LogSeverity.Info);
            }

            ActivateTimer().GetAwaiter().GetResult();
        }
        #endregion

        #region Public Methods
        public async Task<List<Game>> FetchGames()
        {
            var json = await WebService.GetStringAsync(BaseApi + futureMatch);

            await LoggindService.Log("Jogos atualizados", GetType(), Discord.LogSeverity.Info);

            games = await Json.ToObjectAsync<List<Game>>(json);

            teams = await GetTeams(games);

            var gamesList = new Games { GamesList = games, LastUpdated = DateTime.Now };

            OnGotGames(games);

            await File.WriteAllTextAsync(BotConfig.GamesFile, await Json.StringifyAsync(gamesList));
            return games;
        }

        public async Task<Game> GetSingleMatch(int matchId)
        {
            string url = BaseApi + "matches/" + matchId;
            var result = await WebService.GetStringAsync(url);
            return await Json.ToObjectAsync<Game>(result);
        }

        public async Task AddTeam(string team)
        {
            teams.Add(team);
            var orderedTeam = teams.OrderBy(t => t).ToList();
            string json = await Json.StringifyAsync(orderedTeam);
            await File.WriteAllTextAsync(BotConfig.TeamsFile, json);
        }
        #endregion

        #region Private Methods
        private async Task ActivateTimer()
        {
            double timeSpan = 15;
            TimeSpan diff = TimeSpan.Zero;
            if (firstRun)
            {
                var json = await File.ReadAllTextAsync(BotConfig.GamesFile);
                var games = await Json.ToObjectAsync<Games>(json);
                this.games = games?.GamesList;

                json = await File.ReadAllTextAsync(BotConfig.TeamsFile);
                this.teams = await Json.ToObjectAsync<HashSet<string>>(json);

                if ((games != null) && (games.LastUpdated.AddMinutes(timeSpan) > DateTime.Now))
                    diff = (games.LastUpdated - DateTime.Now) + TimeSpan.FromMinutes(timeSpan);
            }

            await LoggindService.Log($"Timer ativado para {diff}", GetType(), Discord.LogSeverity.Info);
            var timer = new Timer(async (e) =>
            {
                await FetchGames();
            }, null, diff, TimeSpan.FromMinutes(timeSpan));

            firstRun = false;
            this.timer = timer;
        }

        private async Task<HashSet<string>> GetTeams(List<Game> games)
        {
            if (games == null) return new HashSet<string>();
            if (teams == null)
                teams = new HashSet<string>();

            foreach (var item in games)
            {
                teams.Add(item.TeamA);
                teams.Add(item.TeamB);
            }

            string json = await Json.StringifyAsync(teams);
            await File.WriteAllTextAsync(BotConfig.TeamsFile, json);
            return teams;
        }
        #endregion
    }

    public class GamesEventArgs
    {
        public List<Game> Games { get; set; }
    }
}
