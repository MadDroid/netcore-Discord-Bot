using Bot.Services;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using Discord.WebSocket;
using Bot.Models;

namespace Bot.Modules
{
    [Name("Counter Strike")]
    public class CounterStrike : ModuleBase<SocketCommandContext>
    {
        private GamesService gamesService;
        private ReminderService reminderService;

        public CounterStrike(GamesService gamesService, ReminderService reminderService)
        {
            this.gamesService = gamesService;
            this.reminderService = reminderService;
        }
        
        [Summary("Obtém todos os jogos para hoje")]
        [Command("jogos"), RequireUserPermission(Discord.ChannelPermission.SendTTSMessages)]
        public async Task GamesAsync() => await GamesAsync(string.Empty);

        [Summary("Obtém próximos jogos de um time")]
        [Command("jogos"), RequireUserPermission(Discord.ChannelPermission.SendTTSMessages)]
        public async Task GamesAsync([Remainder][Summary("O time para obter os próximos jogos")]string team)
        {
            StringBuilder reply = new StringBuilder();
            var existingTeam = gamesService.Teams.FirstOrDefault(t => t.ToLower() == team.ToLower());
            foreach (var game in gamesService.Games)
            {
                if (string.IsNullOrEmpty(team))
                {
                    if (DateTime.Compare(DateTime.Now.Date, game.MatchDate.Date) == 0)
                    {
                        if (reminderService.Reminders.Any(r => r.Game.MatchId == game.MatchId))
                            reply.AppendLine(game.ToString() + " " + game.MatchDate.ToString("dd/MM/yyyy HH:mm") + " lembrete definido.");
                        else
                            reply.AppendLine(game.ToString() + " " + game.MatchDate.ToString("dd/MM/yyyy HH:mm"));
                    }
                }
                else if (game.TeamA == existingTeam || game.TeamB == existingTeam)
                {
                    if (reminderService.Reminders.Any(r => r.Game.MatchId == game.MatchId))
                        reply.AppendLine(game.ToString() + " " + game.MatchDate.ToString("dd/MM/yyyy HH:mm") + " lembrete definido");
                    else
                        reply.AppendLine(game.ToString() + " " + game.MatchDate.ToString("dd/MM/yyyy HH:mm"));
                }
            }

            if (string.IsNullOrEmpty(reply.ToString()))
            {
                if (string.IsNullOrEmpty(team))
                    await ReplyAsync("Sem próximos jogos para hoje.");
                else if (string.IsNullOrEmpty(existingTeam))
                    await ReplyAsync("Time não encontrado.");
                else
                    await ReplyAsync($"Sem próximos jogos para {existingTeam}");
            }
            else
                await ReplyAsync($"```cpp\n{reply.ToString()}```");

            //await ReplyAsync(string.IsNullOrEmpty(reply.ToString()) ? "Sem próximos jogos ou time não encontrado." : "```cpp\n" + reply.ToString() + "```");

            await LoggindService.Log($"{Context.User.Username}#{Context.User.Discriminator} solicitou {nameof(GamesAsync)}", GetType(), Discord.LogSeverity.Info);
        }

        [Summary("Obtém todos os times registrados")]
        [Command("times"), RequireUserPermission(Discord.ChannelPermission.SendTTSMessages)]
        public async Task TeamsAsync()
        {
            StringBuilder reply = new StringBuilder();
            foreach (var team in gamesService.Teams)
            {
                reply.AppendLine(team);
            }
            await ReplyAsync($"```cpp\n{reply.ToString()}```");

            await LoggindService.Log($"{Context.User.Username}#{Context.User.Discriminator} solicitou {nameof(TeamsAsync)}", GetType(), Discord.LogSeverity.Info);
        }

        [Summary("Adiciona um time aos times registrados")]
        [Command("addtime"), RequireUserPermission(Discord.ChannelPermission.SendTTSMessages)]
        public async Task AddTeamAsync([Remainder]string team)
        {
            if (gamesService.Teams.Any(t => t.ToLower() == team))
                await ReplyAsync("Time existente");
            else
            {
                await gamesService.AddTeam(team);
                await ReplyAsync($"{team} adicionado");
            }
            
        }
        
        [Command("reminder"), Alias("lembrete"), Summary("Adiciona um lembrete para o time especificado"), RequireUserPermission(Discord.ChannelPermission.SendTTSMessages)]
        public async Task ReminderAsync([Remainder]string team)
        {
            //var game = new Game { MatchDate = DateTime.Now.AddMinutes(1), TeamA = "SK Gaming", TeamB = "TeamB"};
            //var response = reminderService.AddReminder(game, "sk gaming");
            var response = await reminderService.AddReminder(team);
            switch (response.Answer)
            {
                case ReminderAnswer.ReminderSet:
                    response.Reminder.HalfTimeTrigger += ReminderTrigger;
                    response.Reminder.TenTimeTrigger += ReminderTrigger;
                    response.Reminder.OnTimeTrigger += ReminderTrigger;
                    response.Reminder.OtherTimeTrigger += ReminderTrigger;

                    await ReplyAsync($"Lembrete definido para {response.Reminder.Team}. Próximo jogo {response.Reminder.Game.ToString()} {response.Reminder.Game.MatchDate.ToString("dd/MM/yyyy HH:mm")}");

                    await LoggindService.Log($"{Context.User.Username}#{Context.User.Discriminator} criou um novo lembrete para {response.Reminder.Team}", GetType(), Discord.LogSeverity.Info);
                    break;
                case ReminderAnswer.ExistingReminder:
                    await ReplyAsync($"Lembrete já definido para {response.Reminder.Team}. Próximo jogo {response.Reminder.Game.ToString()} {response.Reminder.Game.MatchDate.ToString("dd/MM/yyyy HH:mm")}");
                    break;
                case ReminderAnswer.TeamNotFound:
                    await ReplyAsync("team not found");
                    break;
                case ReminderAnswer.NoUpcomingGames:
                    await ReplyAsync($"Sem próximos jogos para {team}. Um lembrete será definido para quando houver um jogo.");
                    break;
                default:
                    break;
            }
        }

        private async void ReminderTrigger(object sender, Reminder.ReminderEventArgs e)
        {
            if (e.TimeSpan == TimeSpan.FromMinutes(30))
            {
                await ReplyAsync($"Jogo {e.Game.ToString()} começará em 30 minutos.");
            }
            else if (e.TimeSpan == TimeSpan.FromMinutes(10))
            {
                await ReplyAsync($"Jogo {e.Game.ToString()} começará em 10 minutos.");
            }
            else if (e.TimeSpan == TimeSpan.FromMinutes(0))
            {
                await ReplyAsync($"Jogo {e.Game.ToString()} está começando.");
            }
            else
            {
                await ReplyAsync($"Jogo {e.Game.ToString()} começará em {e.TimeSpan.ToString("mm\\:ss")} minutos.");
            }
        }

        [Command("remreminder"), Alias("remlembrete"), RequireUserPermission(Discord.ChannelPermission.SendTTSMessages)]
        public async Task RemoveReminderAsync([Remainder]string team)
        {
            team = gamesService.Teams.FirstOrDefault(t => t.ToLower() == team.ToLower());
            if (string.IsNullOrEmpty(team))
                await ReplyAsync("Time não existente");
            else
            {
                var reminder = reminderService.Reminders.FirstOrDefault(r => r.Team == team);
                if (reminder == null)
                    await ReplyAsync($"Sem lembrete definido para {team}");
                else
                {
                    await reminderService.RemoveReminder(reminder);
                    await ReplyAsync($"Lembrete removido para {team}");
                    await LoggindService.Log($"{Context.User.Username}#{Context.User.Discriminator} removeu o lembrete para {team}", GetType(), Discord.LogSeverity.Info);
                }
            }
        }

        [RequireOwner]
        [Command("update")]
        public async Task UpdateAsync()
        {
            await gamesService.FetchGames();
        }
    }
}
