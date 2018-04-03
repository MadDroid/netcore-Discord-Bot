using Bot.Models;
using Bot.Helpers;
using Discord.WebSocket;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Bot.Services
{
    public class ReminderService
    {
        private GamesService gamesService;
        private List<Reminder> reminders = new List<Reminder>();

        public List<Reminder> Reminders => reminders;

        public ReminderService(GamesService gamesService)
        {
            if (!Directory.Exists(BotConfig.DataFolder))
            {
                Directory.CreateDirectory(BotConfig.DataFolder);
                LoggingService.Log("Diretório de dados criado", GetType(), Discord.LogSeverity.Info);
            }

            if (!File.Exists(BotConfig.RemindersFile))
            {
                File.Create(BotConfig.RemindersFile).Dispose();
                LoggingService.Log("Arquivo de lembretes criado", GetType(), Discord.LogSeverity.Info);
            }

            this.gamesService = gamesService;
            this.gamesService.GotGames += GamesService_GotGames;
            LoadReminders().GetAwaiter().GetResult();
        }

        private void GamesService_GotGames(object sender, GamesEventArgs e)
        {
            foreach (var reminder in Reminders)
            {
                var game = e.Games.FirstOrDefault(g => g.MatchId == reminder.Game.MatchId);
                if (game != null)
                {
                    if (game.MatchDate != reminder.Game.MatchDate)
                    {
                        reminder.Update(game);
                    }
                }
            }
        }

        /// <summary>
        /// Adiciona um lembrete para um jogo.
        /// </summary>
        /// <param name="game">O jogo para ser definido um lembrete.</param>
        /// <param name="team">O time para ser defiindo um lembrete.</param>
        /// <returns></returns>
        public async Task<(ReminderAnswer Answer, Reminder Reminder)> AddReminder(Game game, Team team)
        {
            // Verifica se o lembrete já foi definido.
            var existingReminder = Reminders.FirstOrDefault(g => g.Game.TeamAId == team.Id || g.Game.TeamBId == team.Id);
            if (existingReminder != null)
                return (ReminderAnswer.ExistingReminder, existingReminder);

            team = gamesService.Teams.FirstOrDefault(t => t.Id == team.Id);

            // Verifica se foi passado um jogo, caso não, procura pelo time.
            if (game == null)
            {
                // Verifica se o time existe.
                if (team == null)
                    return (ReminderAnswer.TeamNotFound, null);
                game = gamesService.Games.FirstOrDefault(t => t.TeamAId == team.Id || t.TeamBId == team.Id);
                if (game == null)
                    return (ReminderAnswer.NoUpcomingGames, null);
            }

            var reminder = new Reminder(game, team);
            // HACK: Se remove da lista de lembretes quando for acionado o último evento.
            // TODO: Verificar outra possibilidade, já que essa tira a função da propriedade OnTimeTriggered.
            reminder.OnTimeTrigger += async (s, a) => await RemoveReminder(reminder);
            await SaveReminder(reminder);
            return (ReminderAnswer.ReminderSet, reminder);
        }

        public async Task<(ReminderAnswer Answer, Reminder Reminder)> AddReminder(Team team) => await AddReminder(null, team);

        private async Task SaveReminder(Reminder reminder)
        {
            reminders.Add(reminder);
            var json = await Json.StringifyAsync(Reminders);
            await File.WriteAllTextAsync(BotConfig.RemindersFile, json);
        }

        public async Task RemoveReminder(Reminder reminder)
        {
            reminders.Remove(reminder);
            var json = await Json.StringifyAsync(reminders);
            await File.WriteAllTextAsync(BotConfig.RemindersFile, json);
            await LoggingService.Log($"Lembrete para {reminder.Game.ToString()} removido", GetType(), Discord.LogSeverity.Info);
        }

        /// <summary>
        /// Load all reminders from storage and clean everything that has already passed.
        /// </summary>
        /// <returns></returns>
        private async Task LoadReminders()
        {
            var json = await File.ReadAllTextAsync(BotConfig.RemindersFile);
            reminders = await Json.ToObjectAsync<List<Reminder>>(json);
            if (reminders == null)
                reminders = new List<Reminder>();
            await CleanReminders();
        }

        /// <summary>
        /// Clean all reminders that has already passed.
        /// </summary>
        /// <returns></returns>
        public async Task CleanReminders()
        {
            int removed = reminders.RemoveAll(r => r.OnTimeTriggered);

            if (removed > 0)
            {
                var json = await Json.StringifyAsync(Reminders);
                await File.WriteAllTextAsync(BotConfig.RemindersFile, json);
            }
        }
    }

    public class Reminder
    {
        #region Properties
        [JsonIgnore]
        public bool OnTimeTriggered => onTimeTriggered;
        [JsonIgnore]
        public bool TenTimeTriggered => tenTimeTriggered;
        [JsonIgnore]
        public bool HalfTimeTriggered => halfTimeTriggered;
        public Game Game => game;
        public Team Team => team;
        #endregion

        #region Fields
        private Game game;
        private Team team;
        private bool onTimeTriggered;
        private bool tenTimeTriggered;
        private bool halfTimeTriggered;
        private Timer onTimeTimer;
        private Timer tenTimeTimer;
        private Timer halfTimeTimer;
        private Timer otherTimer;
        #endregion

        #region Events
        public event EventHandler<ReminderEventArgs> OnTimeTrigger;
        public event EventHandler<ReminderEventArgs> TenTimeTrigger;
        public event EventHandler<ReminderEventArgs> HalfTimeTrigger;
        public event EventHandler<ReminderEventArgs> OtherTimeTrigger;
        public void OnTrigger(Game game) => OnTimeTrigger?.Invoke(this, new ReminderEventArgs { Game = game, TimeSpan = TimeSpan.FromMinutes(0) });
        public void TenTrigger(Game game) => TenTimeTrigger?.Invoke(this, new ReminderEventArgs { Game = game, TimeSpan = TimeSpan.FromMinutes(10) });
        public void HalfTrigger(Game game) => HalfTimeTrigger?.Invoke(this, new ReminderEventArgs { Game = game, TimeSpan = TimeSpan.FromMinutes(30) });
        public void OtherTrigger(Game game, TimeSpan timeSpan) => OtherTimeTrigger?.Invoke(this, new ReminderEventArgs { Game = game, TimeSpan = timeSpan });
        #endregion

        public Reminder(Game game, Team team)
        {
            // TODO: Verificar possibilidade de usar System.Timers.Timer
            this.game = game;
            this.team = team;
            var diff = game.MatchDate - DateTime.Now;

            // Se diff for negativo, o jogo já aconteceu.
            if (diff < new TimeSpan(0))
            {
                onTimeTriggered = true;
                return;
            }

            onTimeTimer = new Timer((e) =>
            {
                onTimeTriggered = true;
                OnTrigger(game);
                // TODO: Criar lembrete para o próximo jogo
            }, null, diff, new TimeSpan(-1));

            var tenTime = diff - TimeSpan.FromMinutes(10);
            var halfTime = diff - TimeSpan.FromMinutes(30);

            if (tenTime < TimeSpan.FromMinutes(0))
            {
                tenTime = new TimeSpan(-1);
                tenTimeTriggered = true;
                SetOtherTime(game, tenTime);
                LoggingService.Log("Less than Ten minutes left", GetType(), Discord.LogSeverity.Info);
            }

            if (halfTime < TimeSpan.FromMinutes(0))
            {
                halfTime = new TimeSpan(-1);
                if (!tenTimeTriggered)
                    SetOtherTime(game, diff);

                halfTimeTriggered = true;
                LoggingService.Log("Less than Thirty minutes left", GetType(), Discord.LogSeverity.Info);
            }

            tenTimeTimer = new Timer((e) =>
            {
                tenTimeTriggered = true;
                TenTrigger(game);
            }, null, tenTime, new TimeSpan(-1));

            halfTimeTimer = new Timer((e) =>
            {
                halfTimeTriggered = true;
                HalfTrigger(game);
            }, null, halfTime, new TimeSpan(-1));

            LoggingService.Log($"Novo lembrete {game.ToString()}", GetType(), Discord.LogSeverity.Info);
        }

        private void SetOtherTime(Game game, TimeSpan timeSpan)
        {
            otherTimer = new Timer((e) =>
            {
                OtherTrigger(game, timeSpan - TimeSpan.FromSeconds(5));
            }, null, TimeSpan.FromSeconds(10), new TimeSpan(-1));
        }

        internal void Update(Game game)
        {
            if (game.MatchId != Game.MatchId)
                return;

            if (game.MatchDate <= Game.MatchDate)
                return;

            this.game = game;

            var diff = game.MatchDate - DateTime.Now;
            var time = diff - TimeSpan.FromMinutes(30);

            if (time > TimeSpan.FromMinutes(0))
            {
                halfTimeTimer.Change(time, new TimeSpan(-1));
                halfTimeTriggered = false;
            }
            else
            {
                halfTimeTimer.Change(Timeout.Infinite, Timeout.Infinite);
                halfTimeTriggered = true;
            }

            time = diff - TimeSpan.FromMinutes(10);

            if (time > TimeSpan.FromMinutes(0))
            {
                tenTimeTimer.Change(time, new TimeSpan(-1));
                tenTimeTriggered = false;
            }
            else
            {
                tenTimeTimer.Change(Timeout.Infinite, Timeout.Infinite);
                tenTimeTriggered = true;
            }

            if (diff > TimeSpan.FromMinutes(0))
            {
                onTimeTimer.Change(diff, new TimeSpan(-1));
                onTimeTriggered = false;
            }
            else
            {
                onTimeTimer.Change(Timeout.Infinite, Timeout.Infinite);
                onTimeTriggered = true;
            }

            LoggingService.Log($"Updated reminder - MatchId: {game.MatchId}", GetType(), Discord.LogSeverity.Info);
        }

        public class ReminderEventArgs : EventArgs
        {
            public Game Game { get; set; }
            public TimeSpan TimeSpan { get; set; }
        }
    }
}

public enum ReminderAnswer
{
    ReminderSet,
    ExistingReminder,
    TeamNotFound,
    NoUpcomingGames
}
