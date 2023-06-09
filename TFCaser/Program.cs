using System;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;
using SteamAuth;
using System.Threading;
using System.Timers;
using MySql.Data.MySqlClient;
using System.Data.Common;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Generic;
using System.Drawing;
using GeneralDLL;

namespace ConsoleApp1
{
	class Program
	{

        private static string def = "silent -nofriendsui -nochatui -novid -noshader -low -nomsaa -16bpp -nosound -high";

        private static string parsnew = "-silent -nofriendsui -nosteamcontroller -offline -nochatui -single_core -novid -noshader -nofbo -nodcaudio -nomsaa -16bpp -nosound -high";

        private static string V2 = "-window -32bit +mat_disable_bloom 1 +func_break_max_pieces 0 +r_drawparticles 0 -nosync -nosrgb -console -noipx -nojoy +exec autoexec.cfg -nocrashdialog -high -d3d9ex -noforcemparms -noaafonts" +
            " -noforcemaccel -limitvsconst +r_dynamic 0 -noforcemspd +fps_max 3 -nopreload -nopreloadmodels +cl_forcepreload 0 " +
            "-nosound -novid -w 160 -h 160 -nomouse";

        private static string serverConnectionString = "";

		public static string tfPath = "D:\\Games\\steamapps\\common\\Counter-Strike Global Offensive";

		private static object connObj = new object();

        static string assemblyName = "TF2_IDLE_MACHINE";


        private static void SetOnline(int isOnline, int id)
		{
			lock (connObj)
			{
				try
				{
					connection.Open();

					var command = new MySqlCommand("USE tf2; " +
					"Update accounts set isOnline = @isOnline where id = @id", connection);
					command.Parameters.AddWithValue("@isOnline", isOnline);
					command.Parameters.AddWithValue("@id", id);
					int rowCount = command.ExecuteNonQuery();
					connection.Close();
				}
				catch (Exception ex)
				{
					Logger.LogAndWritelineAsync($"[003][{assemblyName}] {ex.Message}");
				}
				finally
				{
					connection.Close();
				}
			}
		}
		

		[DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
		public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint cButtons, uint dwExtraInfo);

		[System.Runtime.InteropServices.DllImport("user32.dll")]
		static extern bool SetCursorPos(int x, int y);						

		[DllImport("user32.dll", SetLastError = true)]
		public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);		

		[DllImport("user32.dll")]
		static extern bool SetWindowText(IntPtr hWnd, string text);

		[DllImport("user32.dll")]
		public static extern UInt32 GetWindowThreadProcessId(IntPtr hwnd, ref Int32 pid);

		[DllImport("user32.dll", SetLastError = true)]
		static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);		

		const uint SWP_NOZORDER = 0x0004;		

		const uint SWP_NOSIZE = 0x0001;

		private static object mainObj = new object();

		private static object threadLockType = new object();

		private static int windowCount = 0;

		private static int windowInARow = 0;

		private static int xOffset = 0;

		private static int yOffset = 0;

		private static int xSize = Convert.ToInt32(160 / GeneralDLL.Monitor.GetWindowsScreenScalingFactor()); //всё равно ставит своё разрешение

		private static int ySize = Convert.ToInt32(160 / GeneralDLL.Monitor.GetWindowsScreenScalingFactor()); //всё равно ставит своё разрешение

		private static MySqlConnection connection = DBUtils.GetDBConnection();

		private static int processStarted = 0;

		private static int timeIdle = 600000; //210 минут 12600000;

		private static int consoleX = 380;
		
		private static int consoleY = 270;

        private static int maxWindowInARow = GeneralDLL.Monitor.realMonitorSizeX / xSize;

		static int minToNewCycle = timeIdle / 60000;

		static uint MOUSEEVENTF_LEFTDOWN = 0x02;

		static uint MOUSEEVENTF_LEFTUP = 0x04;

		static System.Timers.Timer tmr = new System.Timers.Timer();

		static int timerDelayInMins = 1;

		static int timerDelayInSeconds = timerDelayInMins * 1000 * 60;

		static List<string> listSteamLogin = new List<string>();

		static int exceptionsInARow = 0;

		private static bool updatingWasFound = false;
        

        private static void TmrEvent(object sender, ElapsedEventArgs e)
		{
			minToNewCycle -= timerDelayInMins;
			Console.ForegroundColor = ConsoleColor.Green; // устанавливаем цвет	
			Logger.LogAndWritelineAsync($"New cycle after: {minToNewCycle} minutes" +"   "); //пробелы что бы инфа от старой строки не осталось, но не слишком много, а то заедет некст строка		
			Console.ResetColor(); // сбрасываем в стандартный

			if (minToNewCycle <= 1)
			{
				tmr.Enabled = false;
			}
		}

		private static void CheckTime(ref bool k, System.Timers.Timer timer)
		{
			k = true;
			timer.Enabled = false;			
		}

		private static void CheckTimeSteam(ref bool k, System.Timers.Timer timer)
		{
			k = true;
			timer.Enabled = false;
		}

		private static void SetTF2Pos(IntPtr csgoWindow, int xOffset, int yOffset,string login)
		{
			SetWindowPos(csgoWindow, IntPtr.Zero, xOffset, yOffset, xSize, ySize,SWP_NOSIZE | SWP_NOZORDER); //пусть сам выставляет свои 300 по высоте
		}

		private static async Task SetCsgoPosAsync(IntPtr csgoWindow, int xOffsetMonitor, int yOffsetMonitor, string login)
		{
			windowCount += 1;
			windowInARow += 1;
			if (windowInARow >= maxWindowInARow)
			{
				windowInARow = 0;
				yOffset += ySize;
			}
			xOffset = xSize * windowInARow;

            if (xOffsetMonitor == 0 && yOffsetMonitor == 0)
            {				
                minToNewCycle = timeIdle / 60000; //120 сек на открытие кски и ввод строки подключения, потому что после ввода толко начинается отсчёт idletime
                tmr.Enabled = true;

                Console.ForegroundColor = ConsoleColor.Green;
				Console.WriteLine("New cycle");
                Console.ResetColor();

                Console.ForegroundColor = ConsoleColor.Green;  //3 строки - должно быть не тут
				Logger.LogAndWritelineAsync($"New cycle after: {minToNewCycle} minutes" + "   "); //что бы показывало время сразу, а не через минуту
                Console.ResetColor();

            }
            await Task.Run(() => SetTF2Pos(csgoWindow, xOffsetMonitor, yOffsetMonitor,login));
		}


		private static async Task SetTimerToKillTF2(Process steamProc, Process csgoProc, string login, int accid)
		{			
            System.Timers.Timer timer = new System.Timers.Timer(timeIdle);
			timer.Elapsed += (o, e) => KillSteamAndGame(steamProc, csgoProc, accid, login);
            timer.AutoReset = false;
            timer.Enabled = true;
        }

		private static void KillSteamAndGame(Process steamProc, Process gameProc, int accid, string login)
		{
			try
			{
				bool csWasActiveBeforClosing = true;
                try 
                {
					gameProc.Kill();
					Thread.Sleep(1000*50);  //время на синхронизацию
				}
                catch
				{
					csWasActiveBeforClosing = false;
				}

				try
				{
					steamProc.Kill();
				}
				catch { }
				
				SetOnline(0, accid);
				processStarted -= 1;

                if (csWasActiveBeforClosing)
                {
					try
					{
						lock (connObj)
						{
							DateTime date = DateTime.Now;

							connection.Open();
							var com = new MySqlCommand("USE tf2; " +
							"Update accounts set canPlayDate = @canPlayDate where id = @id", connection);
							com.Parameters.AddWithValue("@canPlayDate", date.AddDays(1));
							com.Parameters.AddWithValue("@id", accid);
							com.ExecuteNonQuery();

							connection.Close();
						}
					}
					catch (Exception ex)
					{
                        Logger.LogAndWritelineAsync($"[003][{assemblyName}] [LOGIN: {login}] " + ex);
					}
					finally
					{
						connection.Close();
					}
				}				
			}
			catch (Exception ex)
			{
                Logger.LogAndWritelineAsync($"[{assemblyName}] [LOGIN: {login}] " + ex);
			}
		}

		private static void SetOnlineZero()
		{
			try
			{
				connection.Open();
				var command = new MySqlCommand("USE tf2; " +
				"Update accounts set isOnline = @online0 where isOnline = @online1", connection);
				command.Parameters.AddWithValue("@online0", 0);
				command.Parameters.AddWithValue("@online1", 1);

				command.ExecuteNonQuery();

				connection.Close();
			}
			catch (Exception ex)
			{
                Logger.LogAndWritelineAsync($"[003][{assemblyName}] {ex.Message}");
            }
			finally
			{
				connection.Close();
			}
		}

		private static void StartTF(int currentCycle, int lastCycle) //object state
		{
			int accid = 0;
			string login = "";
			string password = "";
			string secretKey = "";
			bool isOnline = false;
			int steamProcId = 0;
			int csProcId = 0;
			DateTime canPlayDate = default;

			try
			{
				lock (mainObj)
				{
					//записываем данные в переменные
					try
					{						
						connection.Open();
						var command = new MySqlCommand("USE tf2; " +
							"select * from accounts where isOnline = 0 AND NOW() > canPlayDate limit 1", connection);

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
									isOnline = Convert.ToBoolean(Convert.ToInt32(reader.GetString(4)));

									canPlayDate = Convert.ToDateTime(reader.GetString(6));
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

					if(exceptionsInARow!=0)
                        Logger.LogAndWritelineAsync($"Exceptions in a row: {exceptionsInARow}");

					if (exceptionsInARow >= 2)
					{
						try
						{
							DateTime date = DateTime.Now;
							connection.Open();
							var command = new MySqlCommand("USE tf2; " +
							"Update accounts set canPlayDate = @canPlayDate where id = @id", connection);
							command.Parameters.AddWithValue("@canPlayDate", date.AddHours(2));
							command.Parameters.AddWithValue("@id", accid);
							command.ExecuteNonQuery();
							connection.Close();
						}
						catch (Exception ex)
						{
                            Logger.LogAndWritelineAsync($"[003][{assemblyName}] {ex.Message}");
						}
						finally
						{
							exceptionsInARow = 0;
							connection.Close();
						}
                        Logger.LogAndWritelineAsync($"[005][{assemblyName}] Too much exceptions");
						throw new Exception("Abort");
					}

					Steam.KillWrongSteams(listSteamLogin);

                    Process csgoProc = new Process();
					Process steamProc = new Process();
					Process guardProc = new Process();
					Process consoleProcess = new Process();
					ProcessStartInfo processStartInfo = new ProcessStartInfo();
					Thread thread = default;

					processStartInfo.WindowStyle = ProcessWindowStyle.Hidden;
					processStartInfo.FileName = "cmd.exe";
                    processStartInfo.Arguments = string.Format("/C \"{0}\" -noverifyfiles -noreactlogin -login {1} {2} -applaunch 440 -no-browser {3} -x {4} -y {5} {6} {7}", new object[]
                    {
                       @"C:\Program Files (x86)\Steam\steam.exe",
                       login,
                       password,
                       Program.V2,
                       xSize * GeneralDLL.Monitor.GetWindowsScreenScalingFactor() * windowInARow,
                       Program.yOffset * GeneralDLL.Monitor.GetWindowsScreenScalingFactor(),
                       Program.parsnew,
                       Program.def
                    });

                    consoleProcess.StartInfo = processStartInfo;
					consoleProcess.Start();

					IntPtr steamWindow = new IntPtr();
					IntPtr csgoWindow = new IntPtr();
					IntPtr steamGuardWindow = new IntPtr();
					IntPtr consoleWindow = FindWindow(null, assemblyName);

					bool timeIsOverSteam = false;
					System.Timers.Timer tmrSteam = new System.Timers.Timer();
					tmrSteam.Interval = 1000 * 60;
					tmrSteam.Elapsed += (o, e) => CheckTimeSteam(ref timeIsOverSteam, tmrSteam);
					tmrSteam.Enabled = true;								

					while (true)
					{
                        steamWindow = FindWindow(null, "Steam Sign In");
                        if (steamWindow.ToString() != "0" && !listSteamLogin.Contains(steamWindow.ToString()))
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
                            exceptionsInARow += 1;
							Thread.Sleep(1000);
							throw new Exception("Abort");
						}

						Thread.Sleep(100);
					}	

                    try
                    {
						thread.Join();
					}
                    catch { }

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
                                Keyboard.EraseCharacters(steamWindow, 5);
                                Keyboard.TypeText(consoleWindow, steamWindow, codeGuard);
                            }
                        }
                        //если окно стим гварда(логина) было найдено и сейчас уже закрылось. Бывает ошибка и долго висит, пока не появится стим табличка с началом запуска кс
                        // так что надо ждать пока появится это окно, что бы ошибка закрылось и код пошёл дальше
                        if (FindWindow(null, $"Steam Sign In").ToString() == "0" && steamWasDetected == true) 
                        {
                            Logger.LogAndWritelineAsync("[SYSTEM] Guard was successfully completed");
                            Thread.Sleep(3000);
                            break;
                        }
                        Thread.Sleep(1000);
                    }

                    //ну тут понятно если не было найдено окно стима 
                    if (steamWasDetected == false)
                    {
                        steamProc.Kill(); 
                        Logger.LogAndWritelineAsync("[SYSTEM] Cant find Guard window");
                        exceptionsInARow += 1;
                        Thread.Sleep(1000);
                        throw new Exception("Abort");
                    }

                    // тут если гвард был раньше найден, потом закрылся (условия прохождения while выше). А сейчас опять открыт
                    if (steamWasDetected == true && FindWindow(null, $"Steam Sign In").ToString() != "0")
                    {
                        steamProc.Kill(); // если процесс подвисает на время загрузки гварда, никак не убить
                        Logger.LogAndWritelineAsync($"[{assemblyName}] Cant skip Guard window");
                        exceptionsInARow += 1;
                        Thread.Sleep(1000);
                        throw new Exception("Abort");
                    }
                    				

					bool timeIsOver = false;
					System.Timers.Timer tmr2 = new System.Timers.Timer();
                    tmr2.Interval = 1000*30; //для тф2 120 сек многовато мб
                    tmr2.Elapsed += (o, e) => CheckTime(ref timeIsOver, tmr2);
					tmr2.Enabled = true;
					int xOffSave = xOffset;
					int yOffSave = yOffset;
					IntPtr steamSyncWindow = default;

					while (true)
					{
						csgoWindow = FindWindow(null, "Team Fortress 2");
						steamSyncWindow = FindWindow(null, "Steam Dialog");

						// пока под вопросом, это синкхендлер
						//if(steamSyncWindow.ToString() != "0")
      //                  {
						//	Thread.Sleep(1000);
      //                      Logger.LogAndWritelineAsync("[SYSTEM] SYNC handler");
						//	Keyboard.SetForegroundWindow(steamSyncWindow);
						//	Thread.Sleep(500);
						//	SetCursorPos(831, 697);
						//	Thread.Sleep(500);
						//	mouse_event(MOUSEEVENTF_LEFTDOWN, (uint)831, (uint)697, 0, 0);
						//	Thread.Sleep(50);
						//	mouse_event(MOUSEEVENTF_LEFTUP, (uint)831, (uint)697, 0, 0);
						//	Thread.Sleep(100);

						//	SetCursorPos(807, 643);
						//	Thread.Sleep(500);
						//	mouse_event(MOUSEEVENTF_LEFTDOWN, (uint)807, (uint)643, 0, 0);
						//	Thread.Sleep(50);
						//	mouse_event(MOUSEEVENTF_LEFTUP, (uint)807, (uint)643, 0, 0);
						//	Thread.Sleep(100);

						//	SetCursorPos(949, 819);
						//	Thread.Sleep(500);
						//	mouse_event(MOUSEEVENTF_LEFTDOWN, (uint)949, (uint)819, 0, 0);
						//	Thread.Sleep(50);
						//	mouse_event(MOUSEEVENTF_LEFTUP, (uint)949, (uint)819, 0, 0);

						//	Thread.Sleep(500);
						//}

						if (csgoWindow.ToString() != "0")
						{							
							Thread.Sleep(500);
							//ts.Cancel();
							listSteamLogin.Add(steamWindow.ToString());
                            Logger.LogAndWritelineAsync("[SYSTEM] TF2 detected");
                            Logger.LogAndWritelineAsync(new string('-', 20)+$"Current window: {currentCycle}/{lastCycle}");
							GetWindowThreadProcessId(csgoWindow, ref csProcId);
							csgoProc = Process.GetProcessById(csProcId);

							thread = new Thread(delegate () { SetWindowText(csgoWindow, $"tf2_{login}"); });
							thread.Start();
							SetCsgoPosAsync(csgoWindow, xOffset, yOffset,login);
							break;
						}

						if (timeIsOver == true)
                        {
                            if (updatingWasFound == true)
                            {
								Console.ReadKey(); //ждём пока тогл сам закроет
                            }

							try
                            {
								steamProc.Kill();
							}
                            catch
                            {
                                Logger.LogAndWritelineAsync("[203][SYSTEM] Error");
							}
                            Logger.LogAndWritelineAsync("[SYSTEM] No TF2 detected");
							exceptionsInARow += 1;
							Thread.Sleep(1000);
							throw new Exception("Abort");
						}
						Thread.Sleep(100);
					}
					processStarted += 1;
					SetOnline(1, accid);
					exceptionsInARow = 0;
					SetTimerToKillTF2(steamProc, csgoProc, login, accid);
				}
			}
			catch (Exception ex)
			{
                Logger.LogAndWritelineAsync(ex.Message);
                Logger.LogAndWritelineAsync(new string('-', 35));
			}
		}

		private static void Start(int count)
		{
			int i = 0;
			listSteamLogin.Add("0"); //иногда ловил нолики при закрытии на всякий вставляю
			while (true)
            {				
				while (Process.GetProcessesByName("hl2").Length < count) //processStarted < count // 'ЭТА ВЕРСИЯ ДЛЯ ПОДДЕРЖАНИЯ ВСЕГДА N ПОТОКОВ и норм размещения окон
				{
					Thread myThread = new Thread(delegate () { StartTF(Process.GetProcessesByName("hl2").Length + 1, count); });
					myThread.Start();
					myThread.Join();
					i += 1;
				}
				Thread.Sleep(1000);
			}
		}

		private static async Task StartAsync(int count)
		{
			await Task.Run(() => Start(count));
		}

		static void Main(string[] args)
		{
            GeneralDLL.Debugger.CheckDebugger();
            Console.Title = assemblyName;
			Thread.Sleep(100);
			IntPtr consoleWindow = FindWindow(null, assemblyName);			
			SetWindowPos(consoleWindow, IntPtr.Zero, GeneralDLL.Monitor.realMonitorSizeX - consoleX, GeneralDLL.Monitor.realMonitorSizeY - consoleY - 40, consoleX, consoleY, SWP_NOZORDER); 
			Keyboard.SetForegroundWindow(consoleWindow);

			Console.ForegroundColor = ConsoleColor.Red;
            Logger.LogAndWritelineAsync("НАШ СЕРВЕР DISCORD");
            Logger.LogAndWritelineAsync("discord.gg/nRrrpqhRtg");
			Console.ResetColor();

            if (File.Exists($@"{AppDomain.CurrentDomain.BaseDirectory}\License.lic"))
			{
				string key = Subscriber.GetKey();

				if (PcInfo.GetCurrentPCInfo() == key)
				{
                    Subscriber.CheckSubscribe(key, Games.TF);

                    SetOnlineZero();

                    Logger.LogAndWritelineAsync("Write the number of windows tf2: ");
                    int count = Convert.ToInt32(Console.ReadLine());

                    int cycleCount = 0;
                    tmr.Interval = timerDelayInSeconds;
                    tmr.Elapsed += TmrEvent; //делаем за циклом что бы не стакались события
                    bool hasStarted = false;

                    while (true)
                    {
                        Subscriber.CheckSubscribe(key, Games.TF);

                        if (hasStarted == false)
                        {
                            StartAsync(count);
                            hasStarted = true;
                        }

                        cycleCount += 1;

                        Thread.Sleep(timeIdle);
                        windowInARow = 0;
                        windowCount = 0;
                        xOffset = 0;
                        yOffset = 0;
                    }
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

			Console.ReadKey();
		}
	}
}