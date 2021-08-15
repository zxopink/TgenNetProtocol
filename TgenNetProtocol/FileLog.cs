using System;
using System.IO;

namespace TgenNetProtocol
{
    public static class TgenLog
    {
        const string fileName = "Tgen.Log.txt";
        public static bool consolePrint;
        public static void Log(string log)
        {
            if(consolePrint)
                Console.WriteLine(log);
            try
            {
                if (!File.Exists(fileName))
                {
                    var fileStream = File.Create(fileName);
                    fileStream.Close();
                }
                using (StreamWriter w = File.AppendText(fileName))
                {
                    w.WriteLine(log);
                }
            }
            catch (Exception)
            {
                if(consolePrint)
                    Console.WriteLine("An issue has occured with the log file");
            }

        }

        public static void Log(string[] logs)
        {
            if (consolePrint)
                foreach (var log in logs)
                    Console.WriteLine(log);

            try
            {
                if (!File.Exists(fileName))
                {
                    var fileStream = File.Create(fileName);
                    fileStream.Close();
                }
                using (StreamWriter w = File.AppendText(fileName))
                {
                    foreach (var log in logs)
                        w.WriteLine(log);
                }
            }
            catch (Exception)
            {
                if (consolePrint)
                    Console.WriteLine("An issue has occured with the log file");
            }
        }

        public static void Reset()
        {
            try
            {
                if (File.Exists(fileName))
                    File.Delete(fileName);
            }
            catch (Exception)
            {
                Console.WriteLine("An issue has occured with the log file");
            }
        }
    }
}
