using System;
using Discord.Commands;
using Discord;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Discord.WebSocket;
using System.Reflection;
using System.IO;
using Bot.Services;
using System.Threading;

namespace Bot
{
    class Program
    {
        static void Main(string[] args)
            => new Program().MainAsync().GetAwaiter().GetResult();

        private DiscordSocketClient client;
        private CommandService commands;
        private IServiceProvider services;

#if DEBUG
        public static char Prefix = '$';
#else
        public static char Prefix = '!';
#endif

        private async Task MainAsync()
        {
            client = new DiscordSocketClient();
            commands = new CommandService();

            services = new ServiceCollection()
                .AddSingleton(client)
                .AddSingleton(commands)
                .AddSingleton<GamesService>()
                .AddSingleton<LoggindService>()
                .AddSingleton<ReminderService>()
                .BuildServiceProvider();

            services.GetRequiredService<LoggindService>();
            services.GetRequiredService<GamesService>();
            services.GetRequiredService<ReminderService>();

            await RegisterCommandsAsync();

#if DEBUG
            await client.LoginAsync(TokenType.Bot, BotConfig.MadBotToken);
#else
            await client.LoginAsync(TokenType.Bot, BotConfig.CorujaToken);
#endif
            await client.StartAsync();

            await Task.Delay(-1);
        }

        public async Task RegisterCommandsAsync()
        {
            client.MessageReceived += MessageReceived;

            await commands.AddModulesAsync(Assembly.GetEntryAssembly());
        }

        private async Task MessageReceived(SocketMessage arg)
        {
            var message = arg as SocketUserMessage;

            if (message is null || message.Author.IsBot) return;

            int argPos = 0;

            if (message.HasCharPrefix(Prefix, ref argPos) || message.HasMentionPrefix(client.CurrentUser, ref argPos))
            {
                var context = new SocketCommandContext(client, message);
                var result = await commands.ExecuteAsync(context, argPos, services);

                if (!result.IsSuccess)
                    await LoggindService.Log(result.ErrorReason, result.GetType(), LogSeverity.Info);
            }
        }
    }
}
