using LeattyServer.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeattyServer.ServerInfo.Player;

namespace LeattyServer.ServerInfo.Commands
{
    class DonorCommands
    {
        private static Dictionary<string, Delegate> Commands = new Dictionary<string, Delegate>();

        public static int ReloadCommands()
        {
            Commands.Clear();

            Commands.Add("help", new Action<string[], MapleClient>(ShowCommands));

            Commands.OrderBy(x => x.Key);
            return Commands.Count;
        }

        public static bool ProcessCommand(string[] split, MapleClient c)
        {
            if (split.Length == 0)
                return false;

            string command = split[0].ToLower();

            Delegate action;
            Commands.TryGetValue(command, out action);
            if (action != null)
            {
                try
                {
                    action.DynamicInvoke(split, c);
                }
                catch (Exception e)
                {
                    ServerConsole.Debug("Error parsing Donor command: " + command + "\r\n" + e.ToString());
                    c.Account.Character.SendBlueMessage("An error occured while processing your command");
                }
                return true;
            }
            return false;
        }

        public static void ShowCommands(string[] split, MapleClient c)
        {
            c.Account.Character.SendBlueMessage("Donor Commands:");
            foreach (string str in Commands.Keys)
            {
                c.Account.Character.SendBlueMessage("#" + str);
            }
        }
    }
}
