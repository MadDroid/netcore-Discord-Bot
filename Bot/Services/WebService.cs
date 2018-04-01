using System.Threading.Tasks;

namespace Bot.Services
{
    public static class WebService
    {
        /// <summary>
        /// Get a string. 
        /// </summary>
        /// <param name="url">The url to string to get.</param>
        /// <returns></returns>
        public static async Task<string> GetStringAsync(string url)
        {
            using (var client = new System.Net.Http.HttpClient())
                return await client.GetStringAsync(url);
        }
    }
}
