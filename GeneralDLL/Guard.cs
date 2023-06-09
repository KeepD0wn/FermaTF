using SteamAuth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeneralDLL
{
    public class Guard
    {
        /// <summary>
        /// Возвращает guard code
        /// </summary>
        /// <param name="secretKey"></param>
        /// <returns></returns>
        public static string GetGuardCode(string secretKey)
        {
            SteamGuardAccount acc = new SteamGuardAccount();
            acc.SharedSecret = secretKey;
            string codeGuard = acc.GenerateSteamGuardCode();
            return codeGuard;
        }

        /// <summary>
        /// Возвращает guard code асинхронно
        /// </summary>
        /// <param name="secretKey"></param>
        /// <returns></returns>
        public static async Task<string> GetGuardCodeAsync(string secretKey)
        {
            string s = await Task.Run(() => GetGuardCode(secretKey));
            return s;
        }

    }
}
