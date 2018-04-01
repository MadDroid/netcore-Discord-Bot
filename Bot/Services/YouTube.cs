using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Bot.Services
{
    public static class YouTube
    {
        public static async Task<YouTubeInfo> GetVideoInfo(string url)
        {
            return await Task.Run(() =>
            {
                if (Regex.IsMatch(url, @"youtu\.?be"))
                {
                    string[] patterns =
                        {
                        @"youtu\.be\/([^#\&\?]{11})",  // youtu.be/<id>
                        @"\?v=([^#\&\?]{11})",         // ?v=<id>
                        @"\&v=([^#\&\?]{11})",         // &v=<id>
                        @"embed\/([^#\&\?]{11})",      // embed/<id>
                        @"\/v\/([^#\&\?]{11})"         // /v/<id>
	                };

                    foreach (var element in patterns)
                    {
                        var match = Regex.Match(url, element);
                        if (match.Success)
                            return new YouTubeInfo
                            {
                                IsVideo = true,
                                Id = match.Groups[1].Value
                            };
                    }

                    Match playlist = Regex.Match(url, @"list=([^#\&\?]{34})");
                    if(playlist.Success)
                    {
                        return new YouTubeInfo
                        {
                            IsPlaylist = true,
                            Id = playlist.Groups[1].Value
                        };
                    }
                    return null;
                }

                return null;
            });
        }
    }

    public class YouTubeInfo
    {
        public bool IsVideo { get; set; }
        public bool IsPlaylist { get; set; }

        public string Id { get; set; }
        public string Name { get; set; }
    }
}
