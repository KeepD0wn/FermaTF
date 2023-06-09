using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using GeneralDLL;

namespace CloseAllSteamProcess
{
    class Program
    {
        public static string assemblyName = "Close All Steam Process";

        static void Main(string[] args)
        {
            GeneralDLL.Debugger.CheckDebugger();
            Console.Title = assemblyName;

            try
            {
                if (File.Exists($@"{AppDomain.CurrentDomain.BaseDirectory}\License.lic"))
                {
                    string key = Subscriber.GetKey();

                    if (PcInfo.GetCurrentPCInfo() == key)
                    {
                        Subscriber.CheckSubscribe(key, Games.ANY);

                        int closedCount = 0;
                        foreach (Process process in from proc in Process.GetProcesses() where proc.ProcessName == "steam" select proc)
                        {
                            process.Kill();
                            closedCount += 1;
                        }

                        Logger.LogAndWritelineAsync($"Processes closed {closedCount}");
                        Console.WriteLine("Done");
                    }
                    else
                    {
                        Logger.LogAndWritelineAsync($"[014][{assemblyName}] License not found");
                        Thread.Sleep(5000);
                        Environment.Exit(0);
                    }
                }
                else
                {
                    Logger.LogAndWritelineAsync($"[015][{assemblyName}] License not found");
                    Thread.Sleep(5000);
                    Environment.Exit(0);
                }
            }
            catch (Exception ex)
            {
                Logger.LogAndWritelineAsync($"[{assemblyName}] {ex.Message}");
                Thread.Sleep(5000);
                Environment.Exit(0);
            }
            Console.ReadKey();
        }
    }
}
