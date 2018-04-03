using Discord;
using Discord.Audio;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Threading;

namespace Bot.Services
{
    public class AudioService
    {
        private readonly ConcurrentDictionary<ulong, IAudioClient> ConnectedChannels = new ConcurrentDictionary<ulong, IAudioClient>();

        private CancellationTokenSource tokenSource = new CancellationTokenSource();

        private Queue<(IGuild Guild, IMessageChannel Channel, string Link)> queue = new Queue<(IGuild Guild, IMessageChannel Channel, string Link)>();
        private string playing;
        private IMessageChannel lastChannel;
        private IGuild lastGuild;

        public async Task Play(IGuild guild, IMessageChannel channel, string path)
        {
            if (string.IsNullOrEmpty(playing))
            {
                playing = path;
                await channel.SendMessageAsync($"Tocando {Path.GetFileName(path)}");
                lastChannel = channel;
                lastGuild = guild;
                await SendAudioAsync(guild, channel, path);
            }
            else
            {
                queue.Enqueue((guild, channel, path));
                await channel.SendMessageAsync($"{Path.GetFileName(path)} adicionado a fila.");
            }
        }

        public async Task JoinAudio(IGuild guild, IVoiceChannel target)
        {
            if (ConnectedChannels.TryGetValue(guild.Id, out IAudioClient client))
                return;

            if (target.Guild.Id != guild.Id)
                return;

            var audioClient = await target.ConnectAsync();

            if (ConnectedChannels.TryAdd(guild.Id, audioClient))
            {
                await LoggingService.Log($"Connected to voice on {guild.Name}.", GetType(), LogSeverity.Info);
            }
        }

        public async Task LeaveAudio(IGuild guild)
        {
            if (ConnectedChannels.TryRemove(guild.Id, out IAudioClient client))
            {
                tokenSource.Cancel();
                tokenSource = new CancellationTokenSource();
                await client.StopAsync();
                await LoggingService.Log($"Disconnected from voice on {guild.Name}", GetType(), LogSeverity.Info);
            }
        }


        public async Task SendAudioAsync(IGuild guild, IMessageChannel channel, string path)
        {
            if (ConnectedChannels.TryGetValue(guild.Id, out IAudioClient client))
            {
                await LoggingService.Log($"Starting playback of {path} in {guild.Name}", GetType(), LogSeverity.Debug);
                bool finished = false;
                using (var ffmpeg = CreateStream(path))
                using (var stream = client.CreatePCMStream(AudioApplication.Music))
                {
                    try
                    {
                        await ffmpeg.StandardOutput.BaseStream.CopyToAsync(stream, 81920, tokenSource.Token);
                        finished = true;
                    }
                    catch (OperationCanceledException)
                    {
                        await LoggingService.Log("Streaming stoped.", GetType(), LogSeverity.Debug);
                    }
                    finally
                    {
                        await stream.FlushAsync();
                        if(finished)
                            await Next();
                    }
                }
            }
        }

        private Process CreateStream(string path)
        {
            return Process.Start(new ProcessStartInfo
            {
                FileName = "ffmpeg.exe",
                Arguments = $"-hide_banner -loglevel panic -i \"{path}\" -ac 2 -f s16le -ar 48000 pipe:1",
                UseShellExecute = false,
                RedirectStandardOutput = true
            });
        }


        public void Stop()
        {
            playing = string.Empty;
            tokenSource.Cancel();
            tokenSource = new CancellationTokenSource();
        }

        public async Task Next()
        {
            Stop();
            if (queue.TryDequeue(out (IGuild guild, IMessageChannel channel, string link) resources))
            {
                lastChannel = resources.channel;
                await Play(resources.guild, resources.channel, resources.link);
            }
            else
            {
                await lastChannel?.SendMessageAsync($"Fila vazia. Deixando o canal de voz.");
                await LeaveAudio(lastGuild);
                lastGuild = null;
                lastChannel = null;
            }
        }
    }
}
