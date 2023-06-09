using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace GeneralDLL
{
    public class Keyboard
    {
        [DllImport("user32.dll")]
        static extern int LoadKeyboardLayout(string pwszKLID, uint Flags);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern bool PostMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("USER32.DLL")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        const int wmChar = 0x0102;

        const int VK_ENTER = 0x0D;

        const int VK_BACK = 0x08;

        const int WM_KEYDOWN = 0x100;

        /// <summary>
        /// выделяет окно и нажимает на кнопку backspace n-ое кол-во раз
        /// </summary>
        /// <param name="steamWindow"></param>
        /// <param name="count"></param>
        public static void EraseCharacters(IntPtr steamWindow, int count)
        {
            SetForegroundWindow(steamWindow);
            Thread.Sleep(1000);
            for (int i=0;i<count;i++)
            {
                PostMessage(steamWindow, WM_KEYDOWN, VK_BACK, 1);
                Thread.Sleep(200);
            }           
        }

        /// <summary>
        /// Пишет текст в окно
        /// </summary>
        /// <param name="console"></param>
        /// <param name="steamGuardWindow">сюда пишет текст</param>
        /// <param name="str"></param>
        public static void TypeText(IntPtr console, IntPtr steamGuardWindow, string str)
        {
            Keyboard.SwitchLangToEn();
            Thread.Sleep(100); //когда проц загружен нужен делей
            SetForegroundWindow(console);
            Thread.Sleep(100);
            SetForegroundWindow(steamGuardWindow);
            Thread.Sleep(500);
            foreach (char ch in str)
            {
                PostMessage(steamGuardWindow, wmChar, ch, 0);
                Thread.Sleep(100);
            }
            Thread.Sleep(500);
            PostMessage(steamGuardWindow, WM_KEYDOWN, VK_ENTER, 1);
        }

        /// <summary>
        /// меняет язык раскладки в windows на английский
        /// </summary>
        public static void SwitchLangToEn()
        {
            string lang = "00000409";
            int ret = LoadKeyboardLayout(lang, 1);
            PostMessage(GetForegroundWindow(), 0x50, 1, ret);
        }
    }
}
