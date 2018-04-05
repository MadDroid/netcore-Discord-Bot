using Bot.Services;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using Discord.WebSocket;
using Bot.Models;
using Bot.Helpers;

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
        public async Task GamesAsync()
        {
            Team team = null;
            await GamesAsync(team);
        }

        [Summary("Obtém próximos jogos de um time usando o nome")]
        [Command("jogos"), RequireUserPermission(Discord.ChannelPermission.SendTTSMessages)]
        public async Task GamesAsync([Remainder][Summary("O nome do time para obter os próximos jogos")]string name)
        {
            if (GetTeam(name, out Team team))
                await GamesAsync(team);
            else
                await ReplyAsync("Time inexistente");
        }

        [Summary("Obtém próximos jogos de um time usando a id")]
        [Command("jogos")]
        public async Task GamesAsync([Summary("A id do time para obter os próximos jogos")]int id)
        {
            if(GetTeam(id, out Team team))
                await GamesAsync(team);
            else
                await ReplyAsync("Time inexistente");
        }

        public async Task GamesAsync(Team team)
        {
            StringBuilder reply = new StringBuilder();
            Team existingTeam = null;
            if (team != null)
                existingTeam = gamesService.Teams.FirstOrDefault(t => t.Id == team.Id);
            foreach (var game in gamesService.Games)
            {
                // Se o time for nulo, retorna todos os jogos do dia.
                if (team == null)
                {
                    if (DateTime.Compare(DateTime.Now.Date, game.MatchDate.Date) == 0)
                    {
                        if (reminderService.Reminders.Any(r => r.Game.MatchId == game.MatchId))
                            reply.AppendLine(game.ToString() + " " + game.MatchDate.ToString("dd/MM/yyyy HH:mm") + " lembrete definido.");
                        else
                            reply.AppendLine(game.ToString() + " " + game.MatchDate.ToString("dd/MM/yyyy HH:mm"));
                    }
                }
                // Se time não for nulo, retorna todos os jogos do time.
                else if (game.TeamAId == existingTeam.Id || game.TeamBId == existingTeam.Id)
                {
                    if (reminderService.Reminders.Any(r => r.Game.MatchId == game.MatchId))
                        reply.AppendLine(game.ToString() + " " + game.MatchDate.ToString("dd/MM/yyyy HH:mm") + " lembrete definido");
                    else
                        reply.AppendLine(game.ToString() + " " + game.MatchDate.ToString("dd/MM/yyyy HH:mm"));
                }
            }

            // Se o reply for vazio...
            if (string.IsNullOrEmpty(reply.ToString()))
            {
                // Se o time for nulo...
                if (team == null)
                {
                    await ReplyAsync("Sem próximos jogos para hoje.");
                }
                // Se time existente for nulo...
                else if (existingTeam == null)
                {
                    await ReplyAsync("Time não encontrado.");
                }
                else
                    await ReplyAsync($"Sem próximos jogos para {existingTeam.Name}");
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
                // TODO: Adicionar Id
                reply.AppendLine(team.Name);
            }

            // TODO: Conteúdo da mensagem não pode passar de 2000.

            //await ReplyAsync($"```cpp\n{reply.ToString()}```");

            await ReplyAsync("Serviço desativado temporáriamente");

            await LoggindService.Log($"{Context.User.Username}#{Context.User.Discriminator} solicitou {nameof(TeamsAsync)}", GetType(), Discord.LogSeverity.Info);
        }

        //[Summary("Adiciona um time aos times registrados")]
        //[Command("addtime"), RequireUserPermission(Discord.ChannelPermission.SendTTSMessages)]
        //public async Task AddTeamAsync([Remainder]string team)
        //{
        //    if (gamesService.Teams.Any(t => t.ToLower() == team))
        //        await ReplyAsync("Time existente");
        //    else
        //    {
        //        await gamesService.AddTeam(team);
        //        await ReplyAsync($"{team} adicionado");
        //    }

        //}

        [Command("reminder"), Alias("lembrete"), Summary("Adiciona um lembrete para o time especificado"), RequireUserPermission(Discord.ChannelPermission.SendTTSMessages)]
        public async Task ReminderAsync([Remainder]string name)
        {
            if (GetTeam(name, out Team result))
                await ReminderAsync(result);
            else
                await ReplyAsync("Time inexistente");
        }

        public async Task ReminderAsync(int id)
        {
            if (GetTeam(id, out Team team))
                await ReminderAsync(team);
            else
                await ReplyAsync("Time inexistente");
        }

        public async Task ReminderAsync(Team team)
        {
            var response = await reminderService.AddReminder(team);
            switch (response.Answer)
            {
                case ReminderAnswer.ReminderSet:
                    response.Reminder.HalfTimeTrigger += ReminderTrigger;
                    response.Reminder.TenTimeTrigger += ReminderTrigger;
                    response.Reminder.OnTimeTrigger += ReminderTrigger;
                    response.Reminder.OtherTimeTrigger += ReminderTrigger;

                    await ReplyAsync($"Lembrete definido para {response.Reminder.Team.Name}. Próximo jogo {response.Reminder.Game.ToString()} {response.Reminder.Game.MatchDate.ToString("dd/MM/yyyy HH:mm")}");

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
        public async Task RemoveReminderAsync([Remainder]string name)
        {
            if (GetTeam(name, out Team team))
                await RemoveReminderAsync(team);
            else
                await ReplyAsync("Time inexistente");
        }

        [Command("remreminder"), Alias("remlembrete"), RequireUserPermission(Discord.ChannelPermission.SendTTSMessages)]
        public async Task RemoveReminderAsync(int id)
        {
            if (GetTeam(id, out Team team))
                await RemoveReminderAsync(team);
            else
                await ReplyAsync("Time inexistente");
        }

        public async Task RemoveReminderAsync(Team team)
        {
            if (team == null)
                await ReplyAsync("Time não existente");
            else
            {
                var reminder = reminderService.Reminders.FirstOrDefault(r => r.Team.Id == team.Id);
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

        private bool GetTeam(string name, out Team team)
        {
            if(gamesService.Teams.TryGetItem(t => t.Name.ToLower() == name.ToLower(), out Team result))
            {
                team = result;
                return true;
            }

            team = default(Team);
            return false;
        }

        private bool GetTeam(int id, out Team team)
        {
            if (gamesService.Teams.TryGetItem(t => t.Id == id, out Team result))
            {
                team = result;
                return true;
            }

            team = default(Team);
            return false;
        }
    }
}
