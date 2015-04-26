using LeattyServer.Data;
using LeattyServer.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeattyServer.Constants
{
    class ServerConstants
    {
        public static int ExpRate = 5, MesoRate = 3, DropRate = 1, QuestExpRate = 5;
        public static int MonsterSpawnInterval = 9000; //milliseconds
        public static readonly int MaxOffenceValue = 1000; //Todo, figure out a reasonable value
        public static ushort LoginPort = 8484;
        public static ushort ChannelStartPort = 8585;
        public static short CashShopPort = 8787;
        public static byte Channels = 1;
        public static string DatabaseString = @"data source=.\SQLExpress;Database=Leatty;Integrated Security=SSPI;";

        public static ushort Version = 159;
        public static byte SubVersion = 2;       
        public static ulong CWvsKey = 0x41062C0D46D9C53C;

        public static int PingTimeout = 30; //seconds

        public static string dataApiUrl = "http://localhost:8999";

        public static readonly List<byte[]> NexonIp = new List<byte[]>() { 
            new byte[] { 8, 31, 99, 141 },
            new byte[] { 8, 31, 99, 142 },
            new byte[] { 8, 31, 99, 143 },
            new byte[] { 8, 31, 99, 144 },            
            new byte[] { 208, 85, 110, 166 }
        };

        public static readonly byte[] LocalHostIp = new byte[] { 0, 0, 0, 0 };

        public static bool PrintPackets = true;
        public static bool LocalHost = true;

        private const string TAG_EXPRATE = "EXPRate";
        private const string TAG_MESORATE = "MesoRate";
        private const string TAG_DROPRATE = "DropRate";
        private const string TAG_QUEST_EXPRATE = "QuestEXPRate";
        private const string TAG_MOBSPAWN = "MonsterSpawnInterval";
        private const string TAG_LOGINPORT = "LoginPort";
        private const string TAG_CHANNEL_START_PORT = "ChannelStartPort";
        private const string TAG_CHANNEL_COUNT = "Channels";
        private const string TAG_LOCALHOST = "LocalHost";
        private const string TAG_PRINT_PACKETS = "PrintPackets";
        private const string TAG_DB_CONNECTION_STRING = "DBConnectString";        

        public static void LoadFromFile()
        {
            const string path = "./ServerConstants.ini";
            if (File.Exists(path))
            {
                string[] lines = File.ReadAllLines(path);

                Dictionary<string, string> properties = new Dictionary<string, string>();
                foreach (string line in lines)
                {
                    if (line.Length > 2 && line.Contains('=') && !line.StartsWith(";"))
                    {
                        string[] splittedLine = line.Split('=');
                        if (splittedLine.Length == 2)
                        {
                            properties.Add(splittedLine[0], splittedLine[1]);
                        }
                        else if (splittedLine.Length > 2)
                        {
                            properties.Add(splittedLine[0], splittedLine.Fuse(1, "="));
                        }
                    }
                }

                //set settings
                if (properties.ContainsKey(TAG_EXPRATE)) ExpRate = GetInt(properties, TAG_EXPRATE);

                if (properties.ContainsKey(TAG_MESORATE)) MesoRate = GetInt(properties, TAG_MESORATE);
                if (properties.ContainsKey(TAG_DROPRATE)) DropRate = GetInt(properties, TAG_DROPRATE);
                if (properties.ContainsKey(TAG_QUEST_EXPRATE)) QuestExpRate = GetInt(properties, TAG_QUEST_EXPRATE);

                if (properties.ContainsKey(TAG_MOBSPAWN)) MonsterSpawnInterval = GetInt(properties, TAG_MOBSPAWN);

                if (properties.ContainsKey(TAG_LOGINPORT)) LoginPort = (ushort)GetInt(properties, TAG_LOGINPORT);
                if (properties.ContainsKey(TAG_CHANNEL_START_PORT)) ChannelStartPort = (ushort)GetInt(properties, TAG_CHANNEL_START_PORT);
                if (properties.ContainsKey(TAG_CHANNEL_COUNT)) Channels = (byte)GetInt(properties, TAG_CHANNEL_COUNT);

                if (properties.ContainsKey(TAG_PRINT_PACKETS)) PrintPackets = GetBool(properties, TAG_PRINT_PACKETS);
                if (properties.ContainsKey(TAG_LOCALHOST)) LocalHost = GetBool(properties, TAG_LOCALHOST);

                if (properties.ContainsKey(TAG_DB_CONNECTION_STRING)) DatabaseString = GetString(properties, TAG_DB_CONNECTION_STRING);
                  
           
                ServerConsole.Info("ServerConstants.ini loaded");
            }
            else
            {
                ServerConsole.Warning("ServerConstants.ini not found, using default values");
            }
        }

        public static int GetInt(Dictionary<string, string> properties, string key)
        {
            string temp;
            if (properties.TryGetValue(key, out temp))
            {
                int ret;
                int.TryParse(temp, out ret);
                return ret;
            }
            return 0;            
        }

        private static string GetString(Dictionary<string, string> properties, string key)
        {
            string returnString;
            if (properties.TryGetValue(key, out returnString))
                return returnString;
            return String.Empty;
        }

        private static bool GetBool(Dictionary<string, string> properties, string key)
        {
            string temp;
            if (properties.TryGetValue(key, out temp))            
                return "true".Equals(temp, StringComparison.OrdinalIgnoreCase);
            return false;
        }
    }
}
