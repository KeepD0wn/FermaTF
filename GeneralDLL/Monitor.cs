using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace GeneralDLL
{
    public class Monitor
    {
        enum DeviceCap
        {
            VERTRES = 10,
            DESKTOPVERTRES = 117
        }

        [DllImport("gdi32.dll")]
        static extern int GetDeviceCaps(IntPtr hdc, int nIndex);

        [DllImport("User32.dll")]
        static extern IntPtr GetDC(IntPtr hwnd);

        const int DESKTOPVERTRES = 117;

        const int DESKTOPHORZRES = 118;

        private static double screenScalingFactor = GetWindowsScreenScalingFactor();

        private static IntPtr primary = GetDC(IntPtr.Zero);

        /// <summary>
        /// Ширина экрана в пикселях с учётом масштаба в винде
        /// </summary>
        public static int realMonitorSizeX = Convert.ToInt32(GetDeviceCaps(primary, DESKTOPHORZRES) / screenScalingFactor);

        /// <summary>
        /// Высота экрана в пикселях с учётом масштаба в винде
        /// </summary>
        public static int realMonitorSizeY = Convert.ToInt32(GetDeviceCaps(primary, DESKTOPVERTRES) / screenScalingFactor);


        /// <summary>
        /// Возвращает double который говорит о масштабе в винде
        /// </summary>
        /// <param name="percentage"></param>
        /// <returns></returns>
        public static double GetWindowsScreenScalingFactor(bool percentage = true)
        {
            //Create Graphics object from the current windows handle
            Graphics GraphicsObject = Graphics.FromHwnd(IntPtr.Zero);
            //Get Handle to the device context associated with this Graphics object
            IntPtr DeviceContextHandle = GraphicsObject.GetHdc();
            //Call GetDeviceCaps with the Handle to retrieve the Screen Height
            int LogicalScreenHeight = GetDeviceCaps(DeviceContextHandle, (int)DeviceCap.VERTRES);
            int PhysicalScreenHeight = GetDeviceCaps(DeviceContextHandle, (int)DeviceCap.DESKTOPVERTRES);
            //Divide the Screen Heights to get the scaling factor and round it to two decimals
            double ScreenScalingFactor = Math.Round((double)PhysicalScreenHeight / (double)LogicalScreenHeight, 2);
            //If requested as percentage - convert it
            if (percentage)
            {
                ScreenScalingFactor *= 100.0;
            }
            //Release the Handle and Dispose of the GraphicsObject object
            GraphicsObject.ReleaseHdc(DeviceContextHandle);
            GraphicsObject.Dispose();
            //Return the Scaling Factor
            return ScreenScalingFactor / 100;
        }

    }
}
