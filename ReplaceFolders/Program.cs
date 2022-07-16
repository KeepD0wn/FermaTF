using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Threading;
using MySql.Data.MySqlClient;
using System.Configuration;
using System.Data.Common;

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

        static void Main(string[] args)
        {
            Console.Title = "Replace Folders";
            try
            {
                if (true) //File.Exists($@"{AppDomain.CurrentDomain.BaseDirectory}\License.lic") 
                {
                    //string key = "";
                    //using (StreamReader sr = new StreamReader($@"{AppDomain.CurrentDomain.BaseDirectory}\License.lic"))
                    //{
                    //    key = sr.ReadToEnd();
                    //}
                    //key = key.Replace("\r\n", "");

                    //MySqlConnection conn = new MySqlConnection();
                    //try
                    //{
                    //    conn = new MySqlConnection(Properties.Resources.String1);
                    //    conn.Open();

                    //    var com = new MySqlCommand("USE `MySQL-5846`; " +
                    //     "select * from `subs` where keyLic = @keyLic AND subEnd > NOW() AND activeLic = 1 limit 1", conn);
                    //    com.Parameters.AddWithValue("@keyLic", key);

                    //    using (DbDataReader reader = com.ExecuteReader())
                    //    {
                    //        if (reader.HasRows) //тут уходит на else если нет данных
                    //        {

                    //        }
                    //        else
                    //        {
                    //            conn.Close();
                    //            Console.WriteLine("[SYSTEM] License is not active");
                    //            Thread.Sleep(5000);
                    //            Environment.Exit(0);
                    //        }
                    //    }
                    //    conn.Close();
                    //}
                    //catch
                    //{
                    //    conn.Close();
                    //    Console.WriteLine("[SYSTEM][404] Something went wrong!");
                    //    Thread.Sleep(5000);
                    //    Environment.Exit(0);
                    //}
                    //finally
                    //{
                    //    conn.Close();
                    //}

                    if (true) //PcInfo.GetCurrentPCInfo() == key 
                    {
                        string mainPath = @"C:\Program Files (x86)\Steam\userdata";
                        DirectoryInfo dir = new DirectoryInfo(mainPath);
                        int g = 0;
                        foreach (var item in dir.GetDirectories())
                        {
                            DirectoryInfo sourceDir = new DirectoryInfo($@"{AppDomain.CurrentDomain.BaseDirectory}\reference");
                            DirectoryInfo destinationDir = new DirectoryInfo($@"{mainPath}\{item.Name}");
                            CopyDirectory(sourceDir, destinationDir);

                            g += 1;
                            Console.WriteLine("Folders replaced: " + g);
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
                    Console.WriteLine("[SYSTEM] License not found");
                    Thread.Sleep(5000);
                    Environment.Exit(0);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }            
            Console.ReadLine();
        }
    }
}
