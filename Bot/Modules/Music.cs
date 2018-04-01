using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Bot.Modules
{
    public class Music : ModuleBase<SocketCommandContext>
    {

        [Command("play")]
        public async Task PlayAsync(string link)
        {
            if (!string.IsNullOrEmpty(link))
            {
                var result = await Services.YouTube.GetVideoInfo(link);
                if (result != null)
                {
                    await ReplyAsync(result.IsVideo ? $"Video: {result.Id}" : $"Playlist {result.Id}");
                }
            }
        }

        [Command("skip")]
        public async Task SkipAsync()
        {
            // TODO: Skip music
        }

        [Command("stop")]
        public async Task StopAsync()
        {
            // TODO: Stop music
        }

        bool ValidYouTubeLink(string url)
        {
            Regex r = new Regex(@"(https?://(www\.)?youtube\.com/.*v=\w+.*)|(https?://youtu\.be/\w+.*)|(.*src=.https?://(www\.)?youtube\.com/v/\w+.*)|(.*src=.https?://(www\.)?youtube\.com/embed/\w+.*)");
            return r.IsMatch(url);
        }
    }
}
