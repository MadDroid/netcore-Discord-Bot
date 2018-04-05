using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Bot.Services
{
    public static class StorageService
    {
        /// <summary>
        /// Creates a directory in the given path if not exists.
        /// </summary>
        /// <param name="path">The path for the directory.</param>
        /// <returns>True if the directory was created. False if already exists.</returns>
        public static async Task<bool> CreateDirectory(string path)
        {
            if(!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                await LoggindService.Log($"Directory \"{Path.GetFileName(path)}\" created.", typeof(StorageService), Discord.LogSeverity.Info);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Creates a file in the given path if not exists. 
        /// </summary>
        /// <param name="path"></param>
        /// <returns>True if the file was created. False if already exists.</returns>
        public static async Task<bool> CreateFile(string path)
        {
            if (!File.Exists(path))
            {
                File.Create(path).Dispose();
                await LoggindService.Log($"File \"{Path.GetFileName(path)}\" created.", typeof(StorageService), Discord.LogSeverity.Info);
                return true;
            }

            return false;
        }
    }
}
