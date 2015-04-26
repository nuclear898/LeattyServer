using LeattyServer.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeattyServer.ServerInfo.Player;
using LeattyServer.ServerInfo.Map;
using LeattyServer.Data;
using LeattyServer.ServerInfo.Inventory;

namespace LeattyServer.ServerInfo.Commands
{
    public static class PlayerCommands
    {
        private static Dictionary<string, Delegate> Commands = new Dictionary<string, Delegate>();

        public static int ReloadCommands()
        {
            Commands.Clear();

            Commands.Add("ea", new Action<string[], MapleClient>(UnStuck));
            Commands.Add("help", new Action<string[], MapleClient>(ShowCommands));
            

            Commands = Commands.OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value);
            return Commands.Count;
        }

        public static bool ProcessCommand(string[] split, MapleClient c)
        {
            if (split.Length == 0)
                return false;

            string command = split[0].ToLower();

            Delegate action;
            if (Commands.TryGetValue(command, out action))
            {
                try
                {
                    action.DynamicInvoke(split, c);
                }
                catch (Exception e)
                {
                    ServerConsole.Debug("Error parsing Player command: " + command + "\r\n" + e.ToString());
                    c.Account.Character.SendBlueMessage("An error occured while processing your command");
                }
                return true;
            }
            return false;
        }

        private static void UnStuck(string[] split, MapleClient c)
        {
            c.Account.Character.EnableActions();
        }

        public static void ShowCommands(string[] split, MapleClient c)
        {
            c.Account.Character.SendBlueMessage("Player Commands:");
            foreach (string str in Commands.Keys)
            {
                c.Account.Character.SendBlueMessage("@" + str);
            }
        }
    }
}
