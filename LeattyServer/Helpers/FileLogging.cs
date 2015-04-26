using System;
using System.IO;

namespace LeattyServer.Helpers
{
    class FileLogging
    {
        private static readonly object Sync = new object();

        public static void Log(string fileName, string message)
        {
            lock (Sync)
            {
                if (!Directory.Exists("Logs"))
                {
                    Directory.CreateDirectory("Logs");
                }
                if (!fileName.EndsWith(".txt"))
                    fileName += ".txt";
                using (StreamWriter file = File.AppendText("./Logs/" + fileName))
                {
                    string errorMessage = String.Format("================= Date: {0} =================\r\n", System.DateTime.Now.ToString());
                    errorMessage += message;
                    errorMessage += "\r\n\r\n";

                    file.WriteLine(errorMessage);
                }
            }
        }
    }
}
