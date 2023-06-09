using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeneralDLL
{
    public class Debugger
    {
        /// <summary>
        /// Проверяет программу на дебаг, если да, то выходит
        /// </summary>
        public static void CheckDebugger()
        {
            if (System.Diagnostics.Debugger.IsAttached)
            {                
                Environment.Exit(0);
            }
        }

    }
}
