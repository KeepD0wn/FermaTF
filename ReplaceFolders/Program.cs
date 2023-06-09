using System;
using System.IO;
using System.Threading;
using GeneralDLL;

namespace ReplaceFolders
{
    class Program
    {
        static void CopyDirectory(DirectoryInfo source, DirectoryInfo destination)
        {
            if (!destination.Exists)
            {
                destination.Create();
            }

            // Copy all files.
            FileInfo[] files = source.GetFiles();
            foreach (FileInfo file in files)
            {
                try //если он захочет поменять атрибут у файла которого нет, то и бог с ним
                {
                    DirectoryInfo d = new DirectoryInfo(Path.Combine(destination.FullName, file.Name));
                    if (d.Attributes != FileAttributes.Normal)
                    {
                        File.SetAttributes(d.ToString(), FileAttributes.Normal);
                    }
                }
                catch { }

                file.CopyTo(Path.Combine(destination.FullName,
                    file.Name), true);
            }

            // Process subdirectories.
            DirectoryInfo[] dirs = source.GetDirectories();
            foreach (DirectoryInfo dir in dirs)
            {
                // Get destination directory.
                string destinationDir = Path.Combine(destination.FullName, dir.Name);
                DirectoryInfo k = new DirectoryInfo(destinationDir);
                if (!k.Exists)
                {
                    destination.Create();
                }

                // Call CopyDirectory() recursively.
                CopyDirectory(dir, new DirectoryInfo(destinationDir));
            }
        }

        public static string assemblyName = "Replace Folders";

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

                        string mainPath = @"C:\Program Files (x86)\Steam\userdata";
                        DirectoryInfo dir = new DirectoryInfo(mainPath);
                        int counter = 0;
                        foreach (var item in dir.GetDirectories())
                        {
                            DirectoryInfo sourceDir = new DirectoryInfo($@"{AppDomain.CurrentDomain.BaseDirectory}\reference");
                            DirectoryInfo destinationDir = new DirectoryInfo($@"{mainPath}\{item.Name}");
                            CopyDirectory(sourceDir, destinationDir);

                            counter += 1;
                            Console.WriteLine("Folders replaced: " + counter);
                        }
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
            }
            Console.ReadLine();
        }
    }
}
