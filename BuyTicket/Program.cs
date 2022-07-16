using System;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;
using SteamAuth;
using System.Threading;
using System.Windows.Forms;
using System.Timers;
using MySql.Data.MySqlClient;
using System.Data.Common;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;

namespace ConsoleApp1
{
	class Program
	{
		public enum GetWindow_Cmd : uint

		{

			GW_HWNDFIRST = 0,

			GW_HWNDLAST = 1,

			GW_HWNDNEXT = 2,

			GW_HWNDPREV = 3,

			GW_OWNER = 4,

			GW_CHILD = 5,

			GW_ENABLEDPOPUP = 6,

			WM_GETTEXT = 0x000D
		}

		private static string def = "silent -nofriendsui -nochatui -single_core -novid -noshader -nofbo -nodcaudio -nomsaa -16bpp -nosound -high";
		
		private static string parsnew = "-silent -nofriendsui -nochatui -single_core -novid -noshader -nofbo -nodcaudio -nomsaa -16bpp -nosound -high";
		
		private static string V2 = "-window -32bit +mat_disable_bloom 1 +func_break_max_pieces 0 +r_drawparticles 0 -nosync -noipx -nojoy +exec autoexec.cfg -nocrashdialog -high -d3d9ex -noforcemparms -noaafonts" +
			" -noforcemaccel -limitvsconst +r_dynamic 0 -noforcemspd +fps_max 20 -nopreload -nopreloadmodels +cl_forcepreload 0 " +
			"-nosound -novid -w 1400 -h 900 -nosteamcontroller "; //крайне важен пробел в конце		меньше чеи 640х480 нельзя, иначе кску крашит

		private static string serverConnectionString = "";

		public static string csgopath = "D:\\Games\\steamapps\\common\\Counter-Strike Global Offensive";

		private static object connObj = new object();

		private static object logLocker = new object();

		private static void Log(string message)
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

		private static async Task LogAsync(string message)
		{
			await Task.Run(() => Log(message));
		}

		private static async Task LogAndConsoleWritelineAsync(string message)
		{
			Console.WriteLine(message);
			await Task.Run(() => Log(message));
		}

		private static void SetOnline(int isOnline, int id)
		{
			lock (connObj)
			{
				try
				{
					conn.Open();

					var com = new MySqlCommand("USE tf2; " +
					"Update accounts set isOnline = @isOnline where id = @id", conn);
					com.Parameters.AddWithValue("@isOnline", isOnline);
					com.Parameters.AddWithValue("@id", id);
					int rowCount = com.ExecuteNonQuery();
					conn.Close();
				}
				catch (Exception ex)
				{
					LogAndConsoleWritelineAsync(ex.Message);
				}
				finally
				{
					conn.Close();
				}
			}
		}

		private static void LangToEn()
		{
			string lang = "00000409";
			int ret = LoadKeyboardLayout(lang, 1);
			PostMessage(GetForegroundWindow(), 0x50, 1, ret);
		}

		[DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
		public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint cButtons, uint dwExtraInfo);

		[System.Runtime.InteropServices.DllImport("user32.dll")]
		static extern bool SetCursorPos(int x, int y);

		[DllImport("user32.dll")]
		static extern int LoadKeyboardLayout(string pwszKLID, uint Flags);

		[DllImport("user32.dll")]
		public static extern IntPtr GetForegroundWindow();

		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		public static extern bool PostMessage(IntPtr hWnd, int Msg, int wParam, int lParam);		

		[DllImport("user32.dll", SetLastError = true)]
		public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

		[DllImport("USER32.DLL")]
		public static extern bool SetForegroundWindow(IntPtr hWnd);

		[DllImport("user32.dll")]
		static extern bool SetWindowText(IntPtr hWnd, string text);

		[DllImport("user32.dll")]
		public static extern UInt32 GetWindowThreadProcessId(IntPtr hwnd, ref Int32 pid);

		[DllImport("user32.dll", SetLastError = true)]
		static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

		[DllImport("User32.dll")]
		static extern IntPtr GetDC(IntPtr hwnd);

		[DllImport("gdi32.dll", CharSet = CharSet.Auto, SetLastError = true, ExactSpelling = true)]
		public static extern int BitBlt(IntPtr hDc, int x, int y, int nWidth, int nHeight, IntPtr hSrcDC, int xSrc, int ySrc, int dwRop);

		[DllImport("gdi32.dll")]
		static extern int GetDeviceCaps(IntPtr hdc, int nIndex);

		const int VK_ENTER = 0x0D;

		const int WM_KEYDOWN = 0x100;

		const int wmChar = 0x0102;

		const uint SWP_NOZORDER = 0x0004;

		const int DESKTOPVERTRES = 117;

		const int DESKTOPHORZRES = 118;

		const uint SWP_NOSIZE = 0x0001;

		private static object mainObj = new object();

		private static object relogLock = new object();

		private static object threadLockType = new object();

		private static int windowCount = 0;

		private static int windowInARow = 0;

		private static int xOffset = 0;

		private static int yOffset = 0;

		private static int xSize = 160; //всё равно ставит своё разрешение

		private static int ySize = 160; //всё равно ставит своё разрешение

		private static MySqlConnection conn = DBUtils.GetDBConnection();

		private static int processStarted = 0;

		private static List<Process> listCsgo = new List<Process>();

		private static List<Process> listSteam = new List<Process>();

		private static int timeIdle = 600000; //210 минут 12600000;

		private static int consoleX = 380;
		
		private static int consoleY = 270;

		private static int currentCycle = 0;

		private static IntPtr primary = GetDC(IntPtr.Zero);

		private static int monitorSizeX = GetDeviceCaps(primary, DESKTOPHORZRES);

		private static int monitorSizeY = GetDeviceCaps(primary, DESKTOPVERTRES);

		private static int maxWindowInARow = monitorSizeX / xSize;

		static int minToNewCycle = timeIdle / 60000;

		static uint MOUSEEVENTF_LEFTDOWN = 0x02;

		static uint MOUSEEVENTF_LEFTUP = 0x04;

		static System.Timers.Timer tmr = new System.Timers.Timer();

		static int timerDelayInMins = 1;

		static int timerDelayInSeconds = timerDelayInMins * 1000 * 60;

		static List<string> listSteamLogin = new List<string>();

		static int exceptionsInARow = 0;

		private static bool updatingWasFound = false;

		private static int procCount = 0;

		private static void TmrEvent(object sender, ElapsedEventArgs e)
		{
			minToNewCycle -= timerDelayInMins;
			Console.ForegroundColor = ConsoleColor.Green; // устанавливаем цвет	
			LogAndConsoleWritelineAsync($"[SYSTEM] New cycle after: {minToNewCycle} minutes" +"   "); //пробелы что бы инфа от старой строки не осталось, но не слишком много, а то заедет некст строка		
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

		private static IntPtr FindGuard() //, System.Timers.Timer steamGuardTimer
		{
			IntPtr steamGuardWindow;
			steamGuardWindow = FindWindow(null, "Steam Guard - Computer Authorization Required");
			if (steamGuardWindow.ToString() != "0")
			{
				LogAndConsoleWritelineAsync("[SYSTEM] Steam Guard detected");
				return steamGuardWindow;
			}

			steamGuardWindow = FindWindow(null, "Steam Guard — Необходима авторизация компьютера");
			if (steamGuardWindow.ToString() != "0")
			{
				LogAndConsoleWritelineAsync("[SYSTEM] Steam Guard detected");
				return steamGuardWindow;
			}

			return IntPtr.Zero;
		}

		private static void CheckGuardClosed(IntPtr steamGuardWindow, Process steamProc, IntPtr console, int accid, string codeGuard)
		{
			Thread.Sleep(8000);
			IntPtr newGuard = FindGuard(); //новое окно с дефолтным именем, а если энтр не нажат, то имя гвар_айди и не детектит тогда
			if (newGuard.ToString() != "0")
			{
				Thread.Sleep(1000);
				lock (threadLockType)
				{
					TypeText(console, newGuard, codeGuard);
				}

				Thread.Sleep(3000);
				IntPtr newGuard1 = FindGuard();
				if (newGuard1.ToString() != "0")
				{
					LogAndConsoleWritelineAsync("Steam Guard still on");
					LogAndConsoleWritelineAsync(new string('-', 35));
					//try
					//{
					//	listSteamLogin.Remove(steamProc.MainWindowHandle.ToString());
					//}
					//catch { }
					try
                    {
						steamProc.Kill(); //TODO: проверять существует ли 
					}
                    catch 
					{
						LogAndConsoleWritelineAsync("[201][SYSTEM] Error");
					}
					//listSteam.Remove(steamProc);
					exceptionsInARow += 1;
					Thread.Sleep(1000);
					throw new Exception("Abort");
				}
			}
						
			IntPtr steamWarning = FindWindow(null, "Steam - Warning"); //тут убрали пробелы, тк он не детектит
			if (steamWarning.ToString() != "0")
			{
				LogAndConsoleWritelineAsync("[SYSTEM] Steam Warning");
				LogAndConsoleWritelineAsync(new string('-', 35));
				
				//try
				//{
				//	listSteamLogin.Remove(steamProc.MainWindowHandle.ToString());
				//}
				//catch { }
				try
				{
					steamProc.Kill(); //TODO: проверять существует ли 
				}
				catch
				{
					LogAndConsoleWritelineAsync("[202][SYSTEM] Error");
				}
				//listSteam.Remove(steamProc);
				exceptionsInARow += 1;
				Thread.Sleep(1000);
				throw new Exception("Abort");
			}
		}

		private static async Task CheckGuardClosedAsync(IntPtr steamGuardWindow, Process steamProc, IntPtr console, int accid, string codeGuard)
		{
			await Task.Run(() => CheckGuardClosed(steamGuardWindow, steamProc, console, accid, codeGuard));
		}

		private static string GetGuardCode(string secretKey)
		{
			SteamGuardAccount acc = new SteamGuardAccount();
			acc.SharedSecret = secretKey;
			string codeGuard = acc.GenerateSteamGuardCode();
			return codeGuard;
		}
				

		private static async Task<string> GetGuardCodeAsync(string secretKey)
		{
			string s = await Task.Run(() => GetGuardCode(secretKey));
			return s;
		}		

		private static void SetCsgoPos(IntPtr csgoWindow, int xOffset, int yOffset,string login)
		{
			SetWindowPos(csgoWindow, IntPtr.Zero, xOffset, yOffset, 1000, 1000, SWP_NOZORDER); //пусть сам выставляет свои 300 по высоте
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

                Console.ForegroundColor = ConsoleColor.Green; // устанавливаем цвет		
				LogAndConsoleWritelineAsync("[SYSTEM] New cycle");
                Console.ResetColor(); // сбрасываем в стандартный

                Console.ForegroundColor = ConsoleColor.Green;  //3 строки - должно быть не тут
				LogAndConsoleWritelineAsync($"[SYSTEM] New cycle after: {minToNewCycle} minutes" + "   "); //что бы показывало время сразу, а не через минуту
                Console.ResetColor();

            }
            await Task.Run(() => SetCsgoPos(csgoWindow, xOffsetMonitor, yOffsetMonitor,login));
		}

		private static void TypeInCsgo(Process steamProc, string login, int accid, IntPtr console, int xOff, int yOff)
		{
			IntPtr csgoWin = FindWindow(null, $"tf2_{login}");
			if (csgoWin.ToString() != "0")
			{
				lock (threadLockType) //тут снимаем фокус с кски
				{					
					Thread.Sleep(100);
					//SetForegroundWindow(csgoWin);
					int xCs = xOff + 40; //+ отступ в зависимости от окна
					int yCs = yOff + 160;
					int x = monitorSizeX - 265;
					int y = monitorSizeY - 300;

					//клик по окну
					SetCursorPos(xCs, yCs);
					mouse_event(MOUSEEVENTF_LEFTDOWN, (uint)xCs, (uint)yCs, 0, 0);
					mouse_event(MOUSEEVENTF_LEFTUP, (uint)xCs, (uint)yCs, 0, 0);
					Thread.Sleep(500);

					//потом 1 по консоли
					SetForegroundWindow(console);
					SetCursorPos(x, y);
					mouse_event(MOUSEEVENTF_LEFTDOWN, (uint)x, (uint)y, 0, 0);
					mouse_event(MOUSEEVENTF_LEFTUP, (uint)x, (uint)y, 0, 0);
				
					//тут уже вписывает коннект в консоль
					// пока в комент, тк научился менять конфиг и уже не нужно
					//Thread.Sleep(500);
					//string setSize = $"mat_setvideomode {xSize} {ySize} 1";
					//foreach (char ch in setSize)
					//{
					//	PostMessage(csgoWin, wmChar, ch, 0);
					//	Thread.Sleep(50);
					//}
					//PostMessage(csgoWin, WM_KEYDOWN, VK_ENTER, 1);

					Thread.Sleep(500);
					LangToEn();
					foreach (char ch in serverConnectionString)
					{
						PostMessage(csgoWin, wmChar, ch, 0);
						Thread.Sleep(50);
					}
					Thread.Sleep(500);
					PostMessage(csgoWin, WM_KEYDOWN, VK_ENTER, 1);
					Thread.Sleep(500);
				}				

			}
		}

		private static async Task TypeInCsgoAsync(Process steamProc, Process csgoProc, string login, int accid, IntPtr console, int xOff,int yOff)
		{
			//await Task.Delay(100000); 
			//Task t = Task.Run(() => TypeInCsgo(steamProc, login, accid, console, xOff, yOff));
            System.Timers.Timer timer = new System.Timers.Timer(timeIdle);
			timer.Elapsed += (o, e) => KillCsSteam(steamProc, csgoProc, accid, login);
            timer.AutoReset = false;
            timer.Enabled = true;
        }

		public static Color GetColorAt(System.Drawing.Point location)
		{
			var screenPixel = new Bitmap(1, 1, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
			using (var gdest = Graphics.FromImage(screenPixel))
			{
				using (var gsrc = Graphics.FromHwnd(IntPtr.Zero))
				{
					IntPtr hSrcDc = gsrc.GetHdc();
					IntPtr hDc = gdest.GetHdc();
					BitBlt(hDc, 0, 0, 1, 1, hSrcDc, location.X, location.Y, (int)CopyPixelOperation.SourceCopy);
					gdest.ReleaseHdc();
					gsrc.ReleaseHdc();
				}
			}

			return screenPixel.GetPixel(0, 0);
		}

		private static void CheckCsGoStatus(int startX, int startY, IntPtr console, Process csgoProc)
        {
            lock (relogLock)
            {
				Color win1 = GetColorAt(new Point(startX + 95, startY + 14));
				Color win2 = GetColorAt(new Point(startX + 14, startY + 7));
				Color win3 = GetColorAt(new Point(startX + 19, startY + 18));
				if (win1.R == 255 && win1.G == 255 && win1.B == 255 && win2.R == 166 && win2.G == 144 && win2.B == 100 && win3.R == 159 && win3.G == 73 && win3.B == 30) //если да, то это окно кс
				{
					Color menu1 = GetColorAt(new Point(startX + 7, startY + 47));
					Color menu2 = GetColorAt(new Point(startX + 10, startY + 90));
					Color menu3 = GetColorAt(new Point(startX + 26, startY + 32));
					Color blueWin = GetColorAt(new Point(startX + 18, startY + 29));
					if (blueWin.R >= 50 && blueWin.R <= 70 && blueWin.G >= 170 && blueWin.G <= 190 && blueWin.B >= 210 && blueWin.B <= 225) //если да, то в главном меню синяя полоска
					{
						LogAndConsoleWritelineAsync("[144][SYSTEM][] Blue window");
					}
					//тут элсе иф и проверка на вак по 190-0-0 ргб и х18у29
					if (menu1.R == 233 && menu1.G == 233 && menu1.B == 233 && menu2.R == 204 && menu2.G == 204 && menu2.B == 204 && menu3.R == 169 && menu3.G == 169 && menu3.B == 169) //если подходит по цветам то в главном меню и без плашки бана, с неё всё становится темнее и цвета другие
					{
						LogAndConsoleWritelineAsync($"Окно для перезахода. Позиция {startX} и {startY}");
						lock (threadLockType)
						{
							LogAndConsoleWritelineAsync("[155][SYSTEM][] Lobbie relog");
							TypeText(console,csgoProc.MainWindowHandle,serverConnectionString);
						}
						Thread.Sleep(5000); //5 сек делея что бы массовы перезаход не ложил комп
					}
				}
			}			
		}

		private static void CheckLobbi(Process csgoProc,string login , IntPtr console, int xOff, int yOff)
        {
			while (true)
			{
				if (FindWindow(null, $"tf2_{login}").ToString() != "0")
				{
					CheckCsGoStatus(xOff, yOff, console, csgoProc);
				}
                else
                {
					break;
                }
				Thread.Sleep(20000);
			}
		}

		private static async Task CheckLobbieAsync(Process csgoProc,string login, IntPtr console, int xOff, int yOff)
		{
			await Task.Run(() => CheckLobbi(csgoProc, login ,console , xOff, yOff));
		}

		private static void KillCsSteam(Process steamProc, Process csgoProc, int accid, string login)
		{
			try
			{
				bool csWasActiveBeforClosing = true;
                try //на всякий в трай, без обработки
                {
					csgoProc.Kill();
					//listCsgo.Remove(csgoProc);
					Thread.Sleep(1000*15); //15 сек на синхронизацию
				}
                catch
				{
					csWasActiveBeforClosing = false;
				}

				try
				{
					steamProc.Kill();
					//listSteam.Remove(steamProc);
				}
				catch { }
				
				SetOnline(0, accid);
				processStarted -= 1;

                if (csWasActiveBeforClosing)
                {
					try //сначала лок, потом трай
					{
						lock (connObj)
						{
							DateTime date = DateTime.Now;

							conn.Open();
							var com = new MySqlCommand("USE tf2; " +
							"Update accounts set canPlayDate = @canPlayDate where id = @id", conn);
							com.Parameters.AddWithValue("@canPlayDate", date.AddDays(1));
							com.Parameters.AddWithValue("@id", accid);
							com.ExecuteNonQuery();

							conn.Close();
						}
					}
					catch (Exception ex)
					{
						LogAndConsoleWritelineAsync($"[950][LOGIN: {login}]" + ex);
					}
					finally
					{
						conn.Close();
					}
				}				
			}
			catch (Exception ex)
			{
				LogAndConsoleWritelineAsync($"[901][LOGIN: {login}]" + ex);
			}
		}

		private static void SetOnlineZero()
		{
			try
			{
				conn.Open();
				var com = new MySqlCommand("USE tf2; " +
				"Update accounts set isOnline = @online0 where isOnline = @online1", conn);
				com.Parameters.AddWithValue("@online0", 0);
				com.Parameters.AddWithValue("@online1", 1);

				com.ExecuteNonQuery();

				conn.Close();
			}
			catch (Exception ex)
			{
				LogAndConsoleWritelineAsync(ex.Message);
			}
			finally
			{
				conn.Close();
			}
		}

		private static void TypeText(IntPtr console, IntPtr targer, string str)
		{
			LangToEn();
			Thread.Sleep(500); //когда проц загружен нужен делей
			SetForegroundWindow(console);
			Thread.Sleep(100);
			SetForegroundWindow(targer);
			Thread.Sleep(1000);
			foreach (char ch in str)
			{
				PostMessage(targer, wmChar, ch, 0);
				Thread.Sleep(100);
			}
			Thread.Sleep(500);
			PostMessage(targer, WM_KEYDOWN, VK_ENTER, 1);
			Thread.Sleep(100);
			SetForegroundWindow(console);
		}

		private static void CloseSteamPlesh()
        {
            if(FindWindow(null, "Steam Login").ToString() != "0")
            {
				LogAndConsoleWritelineAsync("Плешивый стим найден");
				Thread.Sleep(10000);				
			}			
		}

		private static void StartCsGo(int currentCycle, int lastCycle) //object state
		{
			int accid = 0;

			string login = "";

			string password = "";

			string secretKey = "";

			bool isOnline = false;

			int steamProcId = 0;

			int csProcId = 0;

			DateTime lastDateOnline = default;

			DateTime canPlayDate = default;

			try
			{
				lock (mainObj)
				{
					//записываем данные в переменные
					try
					{						
						conn.Open();
						var com = new MySqlCommand("USE tf2; " +
							"select * from accounts where ticketBought = 0 limit 1", conn);

						using (DbDataReader reader = com.ExecuteReader())
						{
							if (reader.HasRows)
							{
								while (reader.Read())
								{
									accid = Convert.ToInt32(reader.GetString(0));
									LogAndConsoleWritelineAsync($"ID: {accid}");

									login = reader.GetString(1);
									LogAndConsoleWritelineAsync($"Login: {login}");

									password = reader.GetString(2);
									secretKey = reader.GetString(3);
									isOnline = Convert.ToBoolean(Convert.ToInt32(reader.GetString(4)));

									lastDateOnline = Convert.ToDateTime(reader.GetString(5));
									LogAndConsoleWritelineAsync($"Last online: {lastDateOnline}");

									canPlayDate = Convert.ToDateTime(reader.GetString(6));
								}
							}
							else
							{
								throw new Exception("[902][SYSTEM] No suitable data");
							}
						}
						conn.Close();
					}
					catch (Exception ex)
					{
						conn.Close();
						LogAndConsoleWritelineAsync(ex.Message);
						if (ex.Message == "[902][SYSTEM] No suitable data")
						{
							LogAndConsoleWritelineAsync("Wait until all accounts are closed");
							Console.ReadLine();
							Environment.Exit(0);
						}
					}

					if (exceptionsInARow != 0)
						LogAndConsoleWritelineAsync($"Exceptions in a row: {exceptionsInARow}");

					if (exceptionsInARow >= 2)
					{
						try
						{
							DateTime date = DateTime.Now;
							conn.Open();
							var com1 = new MySqlCommand("USE tf2; " +
							"Update accounts set canPlayDate = @canPlayDate where id = @id", conn);
							com1.Parameters.AddWithValue("@canPlayDate", date.AddHours(2));
							com1.Parameters.AddWithValue("@id", accid);
							com1.ExecuteNonQuery();
							conn.Close();
						}
						catch (Exception ex)
						{
							LogAndConsoleWritelineAsync(ex.Message);
						}
						finally
						{
							exceptionsInARow = 0;
							conn.Close();
						}
						LogAndConsoleWritelineAsync("[066][SYSTEM] Too much exceptions");
						throw new Exception("Abort");
					}

					//перед запуском проверяем что бы не было заблудившихся стим логинов, что бы они не руинили заходы
					while (FindWindow(null, "Steam Login").ToString() != "0" && !listSteamLogin.Contains(FindWindow(null, "Steam Login").ToString()))
                    {
                        IntPtr wrongSteamWindow = FindWindow(null, "Steam Login");
                        int WrongX = 0;
                        GetWindowThreadProcessId(wrongSteamWindow, ref WrongX);
                        Process WrongSteamLogingProc = Process.GetProcessById(WrongX);
						LogAndConsoleWritelineAsync("[059] Wrong Steam Login Killed");
                        WrongSteamLogingProc.Kill();
                        Thread.Sleep(1000);
                    }


                    Process csgoProc = new Process();
					Process steamProc = new Process();
					Process guardProc = new Process();
					Process process = new Process();
					ProcessStartInfo processStartInfo = new ProcessStartInfo();
					Thread myThread7 = default;

					processStartInfo.WindowStyle = ProcessWindowStyle.Hidden;
					processStartInfo.FileName = "cmd.exe";					
                    processStartInfo.Arguments = string.Format("/C \"{0}\" -login {1} {2} -applaunch 440 -language {3}{4}-x {5} -y {6} {7} {8}", new object[]
                    {
                       @"C:\Program Files (x86)\Steam\steam.exe",
                      login,
                       password,
                       accid,
                       Program.V2,
                       0,
                       0,
                       Program.parsnew,
                       Program.def
                    });

                    process.StartInfo = processStartInfo;
					process.Start();

					IntPtr steamWindowLogin = new IntPtr();
					IntPtr csgoWindow = new IntPtr();
					IntPtr steamGuardWindow = new IntPtr();
					IntPtr console = FindWindow(null, "TF2_TICKET_BUYER");

					bool timeIsOverSteam = false;
					System.Timers.Timer tmrSteam = new System.Timers.Timer();
					tmrSteam.Interval = 1000 * 60;
					tmrSteam.Elapsed += (o, e) => CheckTimeSteam(ref timeIsOverSteam, tmrSteam);
					tmrSteam.Enabled = true;
					lock (threadLockType)
					{
						SetForegroundWindow(console);
					}						

					while (true)
					{
						steamWindowLogin = FindWindow(null, "Вход в Steam");
						if (steamWindowLogin.ToString() != "0" && !listSteamLogin.Contains(steamWindowLogin.ToString())) //ищем стим, которого не было
						{
							LogAndConsoleWritelineAsync("[SYSTEM] Steam detected");

							//listSteamLogin.Add(steamWindow.ToString());

							Thread.Sleep(500);
							GetWindowThreadProcessId(steamWindowLogin, ref steamProcId);
							steamProc = Process.GetProcessById(steamProcId);
							//listSteam.Add(steamProc);
							SetWindowText(steamProc.MainWindowHandle, $"steam_{login}");
							break;
						}

						steamWindowLogin = FindWindow(null, "Steam Login");
						if (steamWindowLogin.ToString() != "0" && !listSteamLogin.Contains(steamWindowLogin.ToString()))
						{
							LogAndConsoleWritelineAsync("[SYSTEM] Steam detected");

							//listSteamLogin.Add(steamWindow.ToString());

							Thread.Sleep(500);
							GetWindowThreadProcessId(steamWindowLogin, ref steamProcId);
							steamProc = Process.GetProcessById(steamProcId);
							//listSteam.Add(steamProc);
							SetWindowText(steamProc.MainWindowHandle, $"steam_{login}");
							break;
						}
                        else
                        {
							Thread.Sleep(500);
                        }


						if (timeIsOverSteam == true)
						{							
							try
							{ //TODO: узнать тот ли стим убивает
								int x = 0;
								GetWindowThreadProcessId(steamWindowLogin, ref x);
								Process steamProcS = Process.GetProcessById(x);
								//listSteamLogin.Remove(steamProcS.MainWindowHandle.ToString());
								LogAndConsoleWritelineAsync("[911] Error");
								steamProcS.Kill();
							}
							catch
							{
								LogAndConsoleWritelineAsync("[219][SYSTEM] Error");
							}
							LogAndConsoleWritelineAsync("[SYSTEM] No Steam detected");
							exceptionsInARow += 1;
							Thread.Sleep(1000);
							throw new Exception("Abort");
						}

						Thread.Sleep(100);
					}					

					bool guardDetected1 = false;
					var codeGuardTask = GetGuardCodeAsync(secretKey);
					DateTime now1 = DateTime.Now;
					while (now1.AddSeconds(25) > DateTime.Now)
					{
						steamGuardWindow = FindWindow(null, "Steam Guard - Computer Authorization Required");
						if (steamGuardWindow.ToString() != "0")
						{
							LogAndConsoleWritelineAsync("[SYSTEM] Steam Guard detected");
							guardDetected1 = true;
							break;
						}

						steamGuardWindow = FindWindow(null, "Steam Guard — Необходима авторизация компьютера");
						if (steamGuardWindow.ToString() != "0")
						{
							LogAndConsoleWritelineAsync("[SYSTEM] Steam Guard detected");
							guardDetected1 = true;
							break;
						}

						Thread.Sleep(100);
                    }

                    try
                    {
						myThread7.Join();
					}
                    catch { }

					codeGuardTask.Wait();
					LogAndConsoleWritelineAsync($"Guard code: {codeGuardTask.Result}");
					if (guardDetected1 == true && steamGuardWindow.ToString() != "0")
					{
						Thread.Sleep(3000); //с 1 секунды до 3 сек что бы на высоких кол-вах не захлёбывался
						lock (threadLockType)
						{
							TypeText(console, steamGuardWindow, codeGuardTask.Result);
						}
					}
					else
					{
						//try
						//{
						//	listSteamLogin.Remove(steamProc.MainWindowHandle.ToString());
						//}
						//catch { }

						steamProc.Kill(); // если процесс подвисает на время загрузки гварда, никак не убить
										  //listSteam.Remove(steamProc);
						LogAndConsoleWritelineAsync("[SYSTEM] No steam Guard detected №2");
						exceptionsInARow += 1;
						Thread.Sleep(1000);
						throw new Exception("Abort");
					}
				
					//var ts = new CancellationTokenSource(); //отменяемый таск
					//CancellationToken ct = ts.Token;
					//Task.Factory.StartNew(() =>
					//{
					//	while (true)
					//	{
					//		IntPtr cs = FindWindow(null, "Updating Counter-Strike: Global Offensive");
					//		IntPtr updError = FindWindow(null, "Steam - Error"); //возникает если обнова была супер быстрая и уже скачалась до того как мы начали её детектить 
					//		if (cs.ToString() != "0" || updError.ToString() !="0")
					//		{
					//			Thread.Sleep(5000);
					//			IntPtr cs2 = FindWindow(null, "Updating Counter-Strike: Global Offensive"); // 2-я проверка через 5 секунд потому что бывают фейк обновы
					//			IntPtr updError2 = FindWindow(null, "Steam - Error"); //возникает если обнова была супер быстрая и уже скачалась до того как мы начали её детектить 
					//			if (cs2.ToString() != "0" || updError2.ToString() != "0")
     //                           {
					//				updatingWasFound = true;
					//				LogAndConsoleWritelineAsync("[SYSTEM] Updating...");
					//				//Thread.Sleep(5000);			

					//				IntPtr toggle = FindWindow(null, "Toggle");
					//				if (toggle.ToString() == "0")
					//				{
					//					Process processTog = new Process();
					//					ProcessStartInfo processStartInfoTog = new ProcessStartInfo();

					//					processStartInfoTog.WindowStyle = ProcessWindowStyle.Minimized;
					//					processStartInfoTog.FileName = "cmd.exe";
					//					processStartInfo.UseShellExecute = true;
					//					processStartInfoTog.Arguments = string.Format("/C \"{0}\" {1}", new object[]
					//					{
					//					$@"{AppDomain.CurrentDomain.BaseDirectory}\Toggle.exe",
					//					lastCycle.ToString()
					//					});

					//					processTog.StartInfo = processStartInfoTog;
					//					processTog.Start();
					//					Environment.Exit(0);
					//				}
					//				else
					//				{
					//					Console.ReadLine(); //ждём пока тогл всё решит за нас
					//				}
					//			}
     //                           else
     //                           {
					//				LogAndConsoleWritelineAsync("[SYSTEM] Fake Updating");
					//			}
					//		}

					//		Thread.Sleep(500);
					//		if (ct.IsCancellationRequested)
					//		{
					//			break;
					//		}
					//	}
					//}, ct);					

					CheckGuardClosed(steamGuardWindow, steamProc, console, accid, codeGuardTask.Result); //тут должно отрабатывать но вообще неочаа

					bool timeIsOver = false;
					System.Timers.Timer tmr2 = new System.Timers.Timer();
                    tmr2.Interval = 1000*60; //для тф2 120 сек многовато мб
                    tmr2.Elapsed += (o, e) => CheckTime(ref timeIsOver, tmr2);
					tmr2.Enabled = true;
					int xOffSave = xOffset;
					int yOffSave = yOffset;

					while (true)
					{
						csgoWindow = FindWindow(null, "Team Fortress 2");
						if (csgoWindow.ToString() != "0")
						{							
							Thread.Sleep(500);
							//ts.Cancel();
							listSteamLogin.Add(steamWindowLogin.ToString());
							LogAndConsoleWritelineAsync("[SYSTEM] TF2 loading");							
							GetWindowThreadProcessId(csgoWindow, ref csProcId);
							csgoProc = Process.GetProcessById(csProcId);

							SetWindowText(csgoWindow, $"tf2_{login}"); //ждёт подгруза кски, занимает се кунд 10. Но если не ждать, то слишком быстро всё
							LogAndConsoleWritelineAsync("[SYSTEM] TF2 detected");
							Thread.Sleep(5000);
							break;
						}

						if (timeIsOver == true)
                        {
                            //try
                            //{
                            //	listSteamLogin.Remove(steamProc.MainWindowHandle.ToString());
                            //}
                            //catch { }	

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
								LogAndConsoleWritelineAsync("[203][SYSTEM] Error");
							}
							LogAndConsoleWritelineAsync("[SYSTEM] No TF2 detected");
							exceptionsInARow += 1;
							Thread.Sleep(1000);
							throw new Exception("Abort");
						}
						Thread.Sleep(100);
					}


                    //ТУТ ПОКУПКА
                    try
                    {
						//выделяем окно
						Thread.Sleep(1000);
						SetCursorPos(11, 200);
						Thread.Sleep(1000);
						mouse_event(MOUSEEVENTF_LEFTDOWN, (uint)11, (uint)11, 0, 0);
						mouse_event(MOUSEEVENTF_LEFTUP, (uint)11, (uint)11, 0, 0);

						Thread.Sleep(1000);
						mouse_event(MOUSEEVENTF_LEFTDOWN, (uint)11, (uint)11, 0, 0);
						mouse_event(MOUSEEVENTF_LEFTUP, (uint)11, (uint)11, 0, 0);

						//клик shop
						Thread.Sleep(1000);
						SetCursorPos(478, 869);
						Thread.Sleep(1000);
						mouse_event(MOUSEEVENTF_LEFTDOWN, (uint)478, (uint)869, 0, 0);
						mouse_event(MOUSEEVENTF_LEFTUP, (uint)478, (uint)869, 0, 0);

						Thread.Sleep(1000);
						mouse_event(MOUSEEVENTF_LEFTDOWN, (uint)478, (uint)869, 0, 0);
						mouse_event(MOUSEEVENTF_LEFTUP, (uint)478, (uint)869, 0, 0);

						//клик items
						Thread.Sleep(2000);
						SetCursorPos(162, 111);
						Thread.Sleep(1000);
						mouse_event(MOUSEEVENTF_LEFTDOWN, (uint)162, (uint)111, 0, 0);
						mouse_event(MOUSEEVENTF_LEFTUP, (uint)162, (uint)111, 0, 0);

						Thread.Sleep(1000);
						mouse_event(MOUSEEVENTF_LEFTDOWN, (uint)162, (uint)111, 0, 0);
						mouse_event(MOUSEEVENTF_LEFTUP, (uint)162, (uint)111, 0, 0);

						//клик search
						Thread.Sleep(1000);
						SetCursorPos(609, 198);
						Thread.Sleep(1000);
						mouse_event(MOUSEEVENTF_LEFTDOWN, (uint)609, (uint)198, 0, 0);
						mouse_event(MOUSEEVENTF_LEFTUP, (uint)609, (uint)198, 0, 0);

						Thread.Sleep(1000);
						mouse_event(MOUSEEVENTF_LEFTDOWN, (uint)609, (uint)198, 0, 0);
						mouse_event(MOUSEEVENTF_LEFTUP, (uint)609, (uint)198, 0, 0);

						//ПИШЕМ TICKET
						Thread.Sleep(1000);
						string setSize = "ticket";
                        foreach (char ch in setSize)
                        {
                            PostMessage(csgoWindow, wmChar, ch, 0);
                            Thread.Sleep(100);
                        }

						//листаем на 3 страницу
						Thread.Sleep(2000);
						SetCursorPos(1228, 669);
						Thread.Sleep(1000);
						mouse_event(MOUSEEVENTF_LEFTDOWN, (uint)1228, (uint)669, 0, 0);
						mouse_event(MOUSEEVENTF_LEFTUP, (uint)1228, (uint)669, 0, 0);

						Thread.Sleep(1000);
						SetCursorPos(1228, 669);
						Thread.Sleep(1000);
						mouse_event(MOUSEEVENTF_LEFTDOWN, (uint)1228, (uint)669, 0, 0);
						mouse_event(MOUSEEVENTF_LEFTUP, (uint)1228, (uint)669, 0, 0);

						//добавляем в корзину
						Thread.Sleep(1000);
						SetCursorPos(846, 480);
						Thread.Sleep(1000);
						mouse_event(MOUSEEVENTF_LEFTDOWN, (uint)846, (uint)480, 0, 0);
						mouse_event(MOUSEEVENTF_LEFTUP, (uint)846, (uint)480, 0, 0);

						//кликаем checkout
						Thread.Sleep(1000);
						SetCursorPos(1163, 868);
						Thread.Sleep(1000);
						mouse_event(MOUSEEVENTF_LEFTDOWN, (uint)1163, (uint)868, 0, 0);
						mouse_event(MOUSEEVENTF_LEFTUP, (uint)1163, (uint)868, 0, 0);

						//авторизация покупки
						Thread.Sleep(5000);
						SetCursorPos(786, 791);
						Thread.Sleep(1000);
						mouse_event(MOUSEEVENTF_LEFTDOWN, (uint)786, (uint)791, 0, 0);
						mouse_event(MOUSEEVENTF_LEFTUP, (uint)786, (uint)791, 0, 0);

						conn.Open();
						var com2 = new MySqlCommand("USE tf2; " +
						"Update accounts set ticketBought = @ticketBought where id = @id", conn);
						com2.Parameters.AddWithValue("@ticketBought", 1);
						com2.Parameters.AddWithValue("@id", accid);
						int rowCount = com2.ExecuteNonQuery();
						procCount += 1;
						exceptionsInARow = 0;

						Thread.Sleep(5000);
												
						try
						{
							Thread.Sleep(1000);
							csgoProc.Kill();

							Thread.Sleep(2000);
							steamProc.Kill();
							Console.WriteLine(new string('-', 20) + $"Ticket Bought: {currentCycle}/{lastCycle}");
						}
						catch { }
					}
                    catch
                    {

                    }
                    finally
                    {
						conn.Close();
					}
					

					//TypeInCsgoAsync(steamProc, csgoProc, login, accid, console, xOffSave, yOffSave);
				}
			}
			catch (Exception ex)
			{
				LogAndConsoleWritelineAsync(ex.Message);
				LogAndConsoleWritelineAsync(new string('-', 35));
			}
		}

		private static void CloseAll()
        {
            try
            {
				foreach (var win in listCsgo)
				{
					win.Kill();
					Thread.Sleep(500);
				}
				listCsgo.Clear();

				LogAndConsoleWritelineAsync("[SYSTEM] All CS:GO killed");
				foreach (var win in listSteam)
				{
					win.Kill();
					//listSteam.Remove(win);
					//ChangeOnline(0, win.);
					processStarted -= 1;
					Thread.Sleep(500);
				}
				//listSteam.Clear();
				LogAndConsoleWritelineAsync("[SYSTEM] All Steam killed");
				LogAndConsoleWritelineAsync(new string('-',35));
			}
            catch(Exception ex)
            {
				LogAndConsoleWritelineAsync(ex.Message);
            }			
		}

		private static void CheckSubscribe(string key)
        {
			MySqlConnection conn = new MySqlConnection();
			try
			{
				conn = new MySqlConnection(Properties.Resources.String1);
				conn.Open();

				var com = new MySqlCommand("USE `MySQL-5846`; " +
				 "select * from `subs` where keyLic = @keyLic AND subEnd > NOW() AND activeLic = 1 limit 1", conn);
				com.Parameters.AddWithValue("@keyLic", key);

				using (DbDataReader reader = com.ExecuteReader())
				{
					if (reader.HasRows) //тут уходит на else если нет данных
					{

					}
					else
					{
						conn.Close();
						LogAndConsoleWritelineAsync("[500][SYSTEM] License is not active");
						Thread.Sleep(5000);
						Environment.Exit(0);
					}
				}
				conn.Close();
			}
			catch
			{
				conn.Close();
				LogAndConsoleWritelineAsync("[SYSTEM][404] Something went wrong!");
				Thread.Sleep(5000);
				Environment.Exit(0);
			}
			finally
			{
				conn.Close();
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
					Thread myThread = new Thread(delegate () { StartCsGo(Process.GetProcessesByName("hl2").Length + 1, count); });
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
			Console.Title = "TF2_TICKET_BUYER";
			Thread.Sleep(100);
			IntPtr conWindow = FindWindow(null, "TF2_TICKET_BUYER");			
			SetWindowPos(conWindow, IntPtr.Zero, monitorSizeX - consoleX, monitorSizeY - consoleY - 40, consoleX, consoleY, SWP_NOZORDER); //вылазит за экран если размер элементов больше 100%			
			SetForegroundWindow(conWindow);
			

            if (true) //(File.Exists($@"{AppDomain.CurrentDomain.BaseDirectory}\License.lic")
			{
				//string key = "";
				//using (StreamReader sr = new StreamReader($@"{AppDomain.CurrentDomain.BaseDirectory}\License.lic"))
				//{
				//    key = sr.ReadToEnd();
				//}
				//key = key.Replace("\r\n", "");

				//CheckSubscribe(key);

				if (true) //PcInfo.GetCurrentPCInfo() == key
				{
					Console.WriteLine("How many Steam accounts to log in: ");
					int count = Convert.ToInt32(Console.ReadLine());

					while (procCount < count)
					{
						Thread myThread = new Thread(delegate () { StartCsGo(procCount + 1, count); });
						myThread.Start();
						myThread.Join();
					}
					Console.WriteLine("Done");
				}
				else
				{
					Console.WriteLine("[SYSTEM] License not found");
					Thread.Sleep(5000);
					Environment.Exit(0);
				}
			}
			else
			{
				LogAndConsoleWritelineAsync("[561][SYSTEM] License not found");
				Thread.Sleep(5000);
				Environment.Exit(0);
			}

			Console.ReadKey();
		}
	}
}