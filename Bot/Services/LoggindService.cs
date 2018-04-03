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
    public class LoggingService
    {
        DiscordSocketClient client;
        CommandService commands;

        public LoggingService(DiscordSocketClient client, CommandService commands)
        {
            this.client = client;
            this.commands = commands;

            client.Log += OnLogAsync;
            commands.Log += OnLogAsync;
        }

        private Task OnLogAsync(LogMessage arg)
        {
            if (!Directory.Exists(BotConfig.LogDirectory))
            {
                Directory.CreateDirectory(BotConfig.LogDirectory);
                Log("Diretótio de logs criado", GetType(), LogSeverity.Info);
            }

            if (!File.Exists(BotConfig.LogFile))
            {
                File.Create(BotConfig.LogFile).Dispose();
                Log("Arquivo de logs criado", GetType(), LogSeverity.Info);
            }

            string logText = $"{DateTime.Now.ToLongTimeString()} [{arg.Severity}] {arg.Source}: {arg.Exception?.ToString() ?? arg.Message}";
            File.AppendAllText(BotConfig.LogFile, logText + "\n");

            return Console.Out.WriteLineAsync(logText);
        }

        public static Task Log(string msg, Type type, LogSeverity severity)
        {
            if (!Directory.Exists(BotConfig.LogDirectory))
            {
                Directory.CreateDirectory(BotConfig.LogDirectory);
                Log("Diretótio de logs criado", typeof(LoggingService), LogSeverity.Info);
            }

            if (!File.Exists(BotConfig.LogFile))
            {
                File.Create(BotConfig.LogFile).Dispose();
                Log("Arquivo de logs criado", typeof(LoggingService), LogSeverity.Info);
            }

            string logText = $"{DateTime.Now.ToLongTimeString()} [{severity}] {type.Name}: {msg}";

            File.AppendAllText(BotConfig.LogFile, logText + "\n");

            return Console.Out.WriteLineAsync(logText);
        }
    }
}
