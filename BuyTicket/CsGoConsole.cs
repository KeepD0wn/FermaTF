using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ConsoleApp1
{
    public class CsGoConsole
    {
        static public List<string> GetLastLines(int amount)
        {
            //check if file exists, if not return null so an error can be displayed
            if (File.Exists($@"{Program.csgopath}\csgo\console.log"))
            {
                int count = 0;
                byte[] buffer = new byte[1];
                List<string> consoleLines = new List<string>();

                using (FileStream fs = new FileStream($@"{Program.csgopath}\csgo\console.log", FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    fs.Seek(0, SeekOrigin.End);

                    while (count <= amount && fs.Position > 0)
                    {
                        fs.Seek(-1, SeekOrigin.Current);
                        fs.Read(buffer, 0, 1);
                        if (buffer[0] == '\n')
                        {
                            count++;
                        }

                        fs.Seek(-1, SeekOrigin.Current); // fs.Read(...) advances the position, so we need to go back again
                    }
                    fs.Seek(1, SeekOrigin.Current); // go past the last '\n'

                    string line;
                    using (StreamReader sr = new StreamReader(fs))
                    {
                        while ((line = sr.ReadLine()) != null)
                            consoleLines.Add(line);
                    }
                }
                return consoleLines;
            }
            else
            {
                return null;
            }
        }          

    }
}
