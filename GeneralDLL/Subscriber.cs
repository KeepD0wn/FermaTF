using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GeneralDLL
{
    /// <summary>
    /// Список игр
    /// </summary>
    public enum Games
    {
        CS,
        TF,
        ANY
    }

    public class Subscriber
    {
        /// <summary>
        /// Ищет подписку для игр tf/cs. Если подписка есть то пройдёт дальше без проблем. Если нету подписки, то напишет и закроет приложение
        /// </summary>
        /// <param name="key">ключ от пк пользователя</param>
        /// <param name="game">tf или cs выбор для какой игры ищем подписку</param>
        public static void CheckSubscribe(string key, Games game)
        {
            Debugger.CheckDebugger();
            MySqlConnection connection = new MySqlConnection();
            try
            {
                connection = new MySqlConnection(Properties.Resources.String1);
                connection.Open();
                var command = new MySqlCommand();

                switch (game)
                {
                    case Games.CS:
                        command = new MySqlCommand("USE subs;select * from `subs` where keyLic = @keyLic AND subEnd > NOW() AND activeLic = 1 limit 1", connection);
                        break;
                    case Games.TF:
                        command = new MySqlCommand("USE subs;select * from `subsTF` where keyLic = @keyLic AND subEnd > NOW() AND activeLic = 1 limit 1", connection);
                        break;
                    case Games.ANY:
                        command = new MySqlCommand("USE subs;select * from `subs` where keyLic = @keyLic AND subEnd > NOW() AND activeLic = 1 " +
                            "union select * from `subsTF` where keyLic = @keyLic AND subEnd > NOW() AND activeLic = 1 limit 1", connection);
                        break;
                }
                
                command.Parameters.AddWithValue("@keyLic", key);

                using (DbDataReader reader = command.ExecuteReader())
                {
                    if (reader.HasRows) 
                    {
                        reader.Read();
                        string dataEnd = reader.GetString(2);
                        Logger.LogAndWritelineAsync($"Subscription will end {dataEnd}");
                        reader.Close();
                    }
                    else
                    {
                        connection.Close();
                        Logger.LogAndWritelineAsync("[010] Wrong license key");
                        Thread.Sleep(5000);
                        Environment.Exit(0);
                    }
                }
                connection.Close();
            }
            catch
            {
                connection.Close();
                Logger.LogAndWritelineAsync("[011] Database not responding");
                Thread.Sleep(5000);
                Environment.Exit(0);
            }
            finally
            {
                connection.Close();
            }
        }      
        
        /// <summary>
        /// читает файл лицензии
        /// </summary>
        /// <returns></returns>
        public static string GetKey()
        {
            string key = "";
            using (StreamReader sr = new StreamReader($@"{AppDomain.CurrentDomain.BaseDirectory}\License.lic"))
            {
                key = sr.ReadToEnd();
            }
            key = key.Replace("\r\n", "");
            return key;

        }

    }
}
