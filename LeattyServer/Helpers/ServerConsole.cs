using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeattyServer.Helpers
{
    public class ServerConsole
    {
        public static void Debug(string msg, params object[] arg)
        {
#if DEBUG
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("[DEBUG] {0}", string.Format(msg, arg));
            Console.ForegroundColor = ConsoleColor.White;
#endif
        }

        public static void Error(string msg, params object[] arg)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("[ERROR] {0}", string.Format(msg, arg));
            Console.ForegroundColor = ConsoleColor.White;
        }

        public static void Warning(string msg, params object[] arg)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("[WARNING] {0}", string.Format(msg, arg));
            Console.ForegroundColor = ConsoleColor.White;
        }

        public static void Info(string msg, params object[] arg)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("[INFO] {0}", string.Format(msg, arg));
        }
    }
}
