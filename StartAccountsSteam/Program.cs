using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using MySql.Data.MySqlClient;
using System.Data.Common;
using GeneralDLL;
using System.IO;
using System.Collections.Generic;

namespace StartAccountsSteam
{
    class Program
    {
        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("USER32.DLL")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern UInt32 GetWindowThreadProcessId(IntPtr hwnd, ref Int32 pid);               

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        static string assemblyName = "Create Folders";

        const uint SWP_NOZORDER = 0x0004;

        private static int consoleX = 380;

        private static int consoleY = 270;

        private static int procCount = 0;

        private static MySqlConnection connection = DBUtils.GetDBConnection();

        private static object threadLockType = new object();

        private static void CheckTimeSteam(ref bool k, System.Timers.Timer timer)
        {
            k = true;
            timer.Enabled = false;
        }

        private static void StartTF2(int currentCycle, int lastCycle)
        {
            try
            {
                string login = "";
                string password = "";
                string secretKey = "";
                int steamProcId = 0;
                int accid = 0;

                try
                {
                    connection.Open();
                    var command = new MySqlCommand("USE tf2; " +
                        "select * from accounts where folderCreated = 0 limit 1", connection);

                    using (DbDataReader reader = command.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                accid = Convert.ToInt32(reader.GetString(0));
                                Logger.LogAndWritelineAsync($"ID: {accid}");

                                login = reader.GetString(1);
                                Logger.LogAndWritelineAsync($"Login: {login}");

                                password = reader.GetString(2);
                                secretKey = reader.GetString(3);
                            }
                        }
                        else
                        {                            
                            throw new NoSuitableDataException("Wait until all accounts will close");
                        }
                    }
                    connection.Close();
                }
                catch (NoSuitableDataException ex)
                {
                    Logger.LogAndWritelineAsync($"[016][{assemblyName}] {ex.Message}");
                    connection.Close();
                    Thread.Sleep(10000);
                    Environment.Exit(0);
                }
                catch (Exception ex)
                {
                    connection.Close();
                    Logger.LogAndWritelineAsync($"[022][{assemblyName}] {ex.Message}");
                }

                Steam.KillWrongSteams(new List<string>());

                Process steamProc = new Process();
                Process consleProc = new Process();
                ProcessStartInfo processStartInfo = new ProcessStartInfo();

                processStartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                processStartInfo.FileName = "cmd.exe";
                processStartInfo.Arguments = string.Format("/C \"{0}\" -noverifyfiles -noreactlogin -login {1} {2} ", new object[]
                {
                       @"C:\Program Files (x86)\Steam\steam.exe",
                       login,
                       password,

                });

                consleProc.StartInfo = processStartInfo;
                consleProc.Start();

                IntPtr steamWindow = new IntPtr();
                IntPtr consoleWindow = FindWindow(null, assemblyName);

                bool timeIsOverSteam = false;
                System.Timers.Timer tmrSteam = new System.Timers.Timer();
                tmrSteam.Interval = 1000 * 40;
                tmrSteam.Elapsed += (o, e) => CheckTimeSteam(ref timeIsOverSteam, tmrSteam);
                tmrSteam.Enabled = true;

                while (true)
                {
                    steamWindow = FindWindow(null, "Steam Sign In");
                    if (steamWindow.ToString() != "0")
                    {
                        Logger.LogAndWritelineAsync("Steam detected");
                        Thread.Sleep(500);
                        GetWindowThreadProcessId(steamWindow, ref steamProcId);
                        steamProc = Process.GetProcessById(steamProcId);
                        break;
                    }
                    else
                    {
                        Thread.Sleep(500);
                    }

                    if (timeIsOverSteam == true)
                    {
                        try
                        { 
                            int x = 0;
                            GetWindowThreadProcessId(steamWindow, ref x);
                            Process steamProcS = Process.GetProcessById(x);
                            Logger.LogAndWritelineAsync($"[007][{assemblyName}] No Steam detected");
                            steamProcS.Kill();
                        }
                        catch
                        {
                            Logger.LogAndWritelineAsync($"[008][{assemblyName}] Error");
                        }
                        Thread.Sleep(1000);
                        throw new Exception("Abort");
                    }
                    Thread.Sleep(100);
                }

                var codeGuard = Guard.GetGuardCode(secretKey);
                Logger.LogAndWritelineAsync($"Guard code: {codeGuard}");

                bool steamWasDetected = false;
                DateTime now = DateTime.Now;

                //именно столько секунд даёт на прогрузку после гварда или когда ошибка expired. Из минусов столько ждать если неправильный пароль 
                while (now.AddSeconds(60) > DateTime.Now)
                {
                    if (FindWindow(null, $"Steam Sign In").ToString() != "0")
                    {
                        steamWasDetected = true;
                        lock (threadLockType)
                        {
                            Keyboard.EraseCharacters(steamWindow,5);
                            Keyboard.TypeText(consoleWindow, steamWindow, codeGuard);
                        }
                    }
                    //если окно стим гварда(логина) было найдено и сейчас уже закрылось. Бывает ошибка и долго висит, пока не появится стим табличка с началом запуска кс
                    // так что надо ждать пока появится это окно, что бы ошибка закрылось и код пошёл дальше
                    if (FindWindow(null, $"Steam Sign In").ToString() == "0" && steamWasDetected == true)
                    {
                        Logger.LogAndWritelineAsync("Guard was successfully completed");
                        Thread.Sleep(3000);
                        break;
                    }
                    Thread.Sleep(1000);
                }

                //ну тут понятно если не было найдено стима переименованное
                if (steamWasDetected == false)
                {
                    steamProc.Kill(); 
                    Logger.LogAndWritelineAsync($"[{assemblyName}] Cant find Guard window");
                    Thread.Sleep(1000);
                    throw new Exception("Abort");
                }

                // тут если гвард был раньше найден, потом закрылся (условия прохождения while выше). А сейчас опять открыт
                if (steamWasDetected == true && FindWindow(null, $"Steam Sign In").ToString() != "0")
                {
                    steamProc.Kill(); // если процесс подвисает на время загрузки гварда, никак не убить
                    Logger.LogAndWritelineAsync($"[{assemblyName}] Cant skip Guard window");
                    Thread.Sleep(1000);
                    throw new Exception("Abort");
                }
                
                while (true)
                {
                   //таймер меняет переменную, тут делаем иф, если проходит внутрь, то аборт
                    steamWindow = FindWindow(null, "Steam");
                    if (steamWindow.ToString() != "0")
                    {
                        Thread.Sleep(500);
                        Logger.LogAndWritelineAsync($"Steam detected");
                        Logger.LogAndWritelineAsync(new string('-', 20) + $"Current window: {currentCycle}/{lastCycle}");
                        steamProc.Kill();
                        Thread.Sleep(500);
                        break;
                    }
                    Thread.Sleep(100);
                }

                try
                {                    
                    connection.Open();
                    var com = new MySqlCommand("USE tf2; " +
                    "Update accounts set folderCreated = @folderCreated where id = @id", connection);
                    com.Parameters.AddWithValue("@folderCreated", 1);
                    com.Parameters.AddWithValue("@id", accid);
                    int rowCount = com.ExecuteNonQuery();
                    procCount += 1;
                }
                catch (Exception ex)
                {
                    Logger.LogAndWritelineAsync($"[003][{assemblyName}] {ex.Message}");
                }
                finally
                {
                    connection.Close();
                }
                Thread.Sleep(1000);
            }
            catch (Exception ex)
            {
                Logger.LogAndWritelineAsync($"[{assemblyName}] {ex.Message}");
                Console.WriteLine(new string('-', 25));
            }
        }       

        static void Main(string[] args)
        {
            GeneralDLL.Debugger.CheckDebugger();
            Console.Title = assemblyName;
            Thread.Sleep(100);
            IntPtr consoleWindow = FindWindow(null, assemblyName);            
            SetWindowPos(consoleWindow, IntPtr.Zero, GeneralDLL.Monitor.realMonitorSizeX - consoleX, GeneralDLL.Monitor.realMonitorSizeY - consoleY - 40, consoleX, consoleY, SWP_NOZORDER); 
            SetForegroundWindow(consoleWindow);

            try
            {
                if (File.Exists($@"{AppDomain.CurrentDomain.BaseDirectory}\License.lic"))
                {
                    string key = Subscriber.GetKey();

                    if (PcInfo.GetCurrentPCInfo() == key)
                    {
                        Subscriber.CheckSubscribe(key, Games.TF);

                        Logger.LogAndWritelineAsync("How many Steam accounts to log in: ");
                        int count = Convert.ToInt32(Console.ReadLine());

                        while (procCount < count)
                        {
                            Thread thread = new Thread(delegate () { StartTF2(procCount+1, count); });
                            thread.Start();
                            thread.Join();
                        }
                        Console.WriteLine("Done");
                    }
                    else
                    {
                        Logger.LogAndWritelineAsync($"[014][{assemblyName}] License not found");
                        Thread.Sleep(5000);
                        connection.Close();
                        Environment.Exit(0);
                    }
                }
                else
                {
                    Logger.LogAndWritelineAsync($"[015][{assemblyName}] License not found");
                    Thread.Sleep(5000);
                    connection.Close();
                    Environment.Exit(0);
                }
            }
            catch (Exception ex)
            {
                Logger.LogAndWritelineAsync($"[{assemblyName}] {ex.Message}");
            }
            finally
            {
                connection.Close();
            }

            Console.ReadLine();          
        }
    }
}
