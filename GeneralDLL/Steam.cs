using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace GeneralDLL
{
    public class Steam
    {
        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        public static extern UInt32 GetWindowThreadProcessId(IntPtr hwnd, ref Int32 pid);

        /// <summary>
        /// убивает все окна с название Steam Sign In, кроме тех которые передаются в аргументе
        /// </summary>
        /// <param name="listSteamLogin"></param>
        public static void KillWrongSteams(List<string> listSteamLogin)
        {
            while (FindWindow(null, "Steam Sign In").ToString() != "0" && !listSteamLogin.Contains(FindWindow(null, "Steam Sign In").ToString()))
            {
                IntPtr wrongSteamWindow = FindWindow(null, "Steam Sign In");
                int WrongProcID = 0;
                GetWindowThreadProcessId(wrongSteamWindow, ref WrongProcID);
                Process WrongSteamLogingProc = Process.GetProcessById(WrongProcID);
                Logger.LogAndWritelineAsync($"[023] Wrong Steam Sign In Killed");
                WrongSteamLogingProc.Kill();
                Thread.Sleep(1000);
            }
        }

    }
}
