using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;
using Discord;
using System.Threading.Tasks;
using System.IO;

namespace Bot.Services
{
    public class LoggindService
    {
        DiscordSocketClient client;
        CommandService commands;

        public LoggindService(DiscordSocketClient client, CommandService commands)
        {
            this.client = client;
            this.commands = commands;

            client.Log += OnLogAsync;
            commands.Log += OnLogAsync;
        }

        private Task OnLogAsync(LogMessage arg)
        {
            StorageService.CreateDirectory(BotConfig.LogDirectory).GetAwaiter().GetResult();
            StorageService.CreateFile(BotConfig.LogFile).GetAwaiter().GetResult();

            string logText = $"{DateTime.Now.ToLongTimeString()} [{arg.Severity}] {arg.Source}: {arg.Exception?.ToString() ?? arg.Message}";
            File.AppendAllText(BotConfig.LogFile, logText + "\n");

            return Console.Out.WriteLineAsync(logText);
        }

        public static Task Log(string msg, Type type, LogSeverity severity)
        {
            StorageService.CreateDirectory(BotConfig.LogDirectory).GetAwaiter().GetResult();
            StorageService.CreateFile(BotConfig.LogFile).GetAwaiter().GetResult();

            string logText = $"{DateTime.Now.ToLongTimeString()} [{severity}] {type.Name}: {msg}";

            File.AppendAllText(BotConfig.LogFile, logText + "\n");

            return Console.Out.WriteLineAsync(logText);
        }
    }
}
