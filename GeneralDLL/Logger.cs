using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeneralDLL
{
    public class Logger
    {
        static object logLocker = new object();

        /// <summary>
        /// Записывает текст в файл log.txt
        /// </summary>
        /// <param name="message"></param>
        static void Log(string message)
        {
            try
            {
                lock (logLocker)
                {
                    StreamWriter connObj = new StreamWriter("log.txt", true);
                    connObj.WriteLine(message + " " + DateTime.Now);
                    connObj.Close();
                }
            }
            catch { }
        }

        /// <summary>
        /// Выводит текст в консоль, а так же пишет в файл log.txt
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public static async Task LogAndWritelineAsync(string message)
        {
            Console.WriteLine(message);
            await Task.Run(() => Log(message));
        }
    }
}
