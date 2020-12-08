using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TgenNetProtocol
{
    public class TgenLog
    {
        const string fileName = "Tgen.Log.txt";
        public static void Log(string log)
        {
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
                Console.WriteLine("An issue has occured with the log file");
            }
        }

        public static void Log(string[] logs)
        {
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
