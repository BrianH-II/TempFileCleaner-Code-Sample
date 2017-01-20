using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace TempFileCleaner
{
    /*
     *  This program cleans the C:\Windows\Temp folder,
     *  as well as the individual users' temp folders,
     *  located at C:\Users\(username)\AppData\Local\Temp.
     *  It then empties the recycle bin.
     *  
     *  Finally, it creates a log of any exceptions that were triggered,
     *  and saves that log as a text file.
     */

    class Program
    {
        enum RecycleFlags : uint
        {
            SHERB_NOCONFIRMATION = 0x00000001,
            SHERB_NOPROGRESSUI = 0x00000002,
            SHERB_NOSOUND = 0x00000004
        }

        [DllImport("Shell32.dll", CharSet = CharSet.Unicode)]
        static extern uint SHEmptyRecycleBin(IntPtr hwnd, string pszRootPath, RecycleFlags dwFlags);

        static List<string> exceptions = new List<string>();

        static void Main(string[] args)
        {
            string logPath = @"S:\My Logs\TempFileCleaner.txt";
            exceptions.Add("The following are all the exceptions that the program TempFileCleaner encountered the last time it was run on "
                + DateTime.Today.ToString("MM.dd.yyyy") + " by " + System.Security.Principal.WindowsIdentity.GetCurrent().Name
                + Environment.NewLine + Environment.NewLine);

            DirectoryInfo info = new DirectoryInfo(@"C:\Windows\Temp");
            exceptions.Add(@"The following exceptions were triggered trying to clean C:\Windows\Temp");
            FindBottomDirectories(info);

            foreach (string path in Directory.GetDirectories(@"C:\Users"))
            {
                exceptions.Add(Environment.NewLine + "The following exceptions were triggered trying to clean " + path + @"\AppData\Local\Temp");
                info = new DirectoryInfo(path + @"\AppData\Local\Temp");
                FindBottomDirectories(info);
            }

            uint success = SHEmptyRecycleBin(IntPtr.Zero, null, RecycleFlags.SHERB_NOCONFIRMATION);

            File.WriteAllLines(logPath, exceptions);
        }

        private static void FindBottomDirectories(DirectoryInfo info)
        {
            try
            {
                if (info.GetDirectories().Length > 0)
                {
                    foreach (DirectoryInfo subInfo in info.GetDirectories())
                    {
                        FindBottomDirectories(subInfo);
                    }
                }

                EmptyDirectory(info);
            }
            catch (Exception e)
            {
                LogException(e);
            }
        }

        private static void EmptyDirectory(DirectoryInfo info)
        {
            foreach (FileInfo file in info.GetFiles())
            {
                try
                {
                    file.Delete();
                }
                catch (Exception e)
                {
                    LogException(e);
                }
            }

            foreach (DirectoryInfo dir in info.GetDirectories())
            {
                try
                {
                    dir.Delete();
                }
                catch (Exception e)
                {
                    LogException(e);
                }
            }
        }

        private static void LogException(Exception e)
        {
            string message = e.Message;
            if (e.Message.Contains("\r\n"))
            {
                message = e.Message.Replace("\r\n", "");
            }
            exceptions.Add(message);
        }
    }
}
