using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

namespace Bot.Helpers
{
    public static class Json
    {
        /// <summary>
        /// Deserialize JSON to object.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value">JSON to deserialize.</param>
        /// <returns></returns>
        public static async Task<T> ToObjectAsync<T>(string value)
        {
            return await Task.Run<T>(() =>
            {
                return JsonConvert.DeserializeObject<T>(value);
            });
        }

        /// <summary>
        /// Serialize object to JSON.
        /// </summary>
        /// <param name="value">Object to be serialized.</param>
        /// <returns></returns>
        public static async Task<string> StringifyAsync(object value)
        {
            return await Task.Run<string>(() =>
            {
                return JsonConvert.SerializeObject(value);
            });
        }

        /// <summary>
        /// Deserialize JSON to object with ca specific token.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value">JSON to deserialize.</param>
        /// <param name="token"></param>
        /// <returns></returns>
        public static async Task<T> ToObjectAsync<T>(string value, string token)
        {
            return await Task.Run<T>(() =>
            {
                return JObject.Parse(value).SelectToken(token).ToObject<T>();
            });
        }
    }
}
