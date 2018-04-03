using Bot.Services;
using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Bot.Modules
{
    public class Music : ModuleBase<SocketCommandContext>
    {
        private AudioService audioService;
        

        public Music(AudioService audioService)
        {
            this.audioService = audioService;
        }

        [Command("play", RunMode = RunMode.Async)]
        public async Task PlayAsync(string link)
        {
            if (!string.IsNullOrEmpty(link))
            {
                var result = await Services.YouTube.GetVideoInfo(link);
                if (result != null)
                {
                    await ReplyAsync(result.IsVideo ? $"Video: {result.Id}" : $"Playlist {result.Id}");
                }
                else
                {
                    if (Uri.TryCreate(link, UriKind.Absolute, out Uri uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps) || uri.Scheme == Uri.UriSchemeFile)
                    {
                        if (Path.GetExtension(link) == ".mp3")
                        {
                            var audioChannel = (Context.User as IVoiceState).VoiceChannel;
                            await audioService.JoinAudio(Context.Guild, audioChannel);
                            await audioService.Play(Context.Guild, Context.Channel, link);
                        }
                    }
                    else
                    {
                        await ReplyAsync("Não foi possível reproduzir.");
                    }
                }
            }
        }

        [Command("skip", RunMode = RunMode.Async)]
        public async Task SkipAsync()
        {
            await audioService.Next();
        }

        [Command("stop")]
        public async Task StopAsync()
        {
            await ReplyAsync("Deixando canal de voz.");
            await audioService.LeaveAudio(Context.Guild);
        }

        bool ValidYouTubeLink(string url)
        {
            Regex r = new Regex(@"(https?://(www\.)?youtube\.com/.*v=\w+.*)|(https?://youtu\.be/\w+.*)|(.*src=.https?://(www\.)?youtube\.com/v/\w+.*)|(.*src=.https?://(www\.)?youtube\.com/embed/\w+.*)");
            return r.IsMatch(url);
        }

        [RequireOwner]
        [Command("join", RunMode = RunMode.Async)]
        public async Task JoinAsync()
        {
            await audioService.JoinAudio(Context.Guild, (Context.User as IVoiceState).VoiceChannel);
        }
    }
}
