using LeattyServer.Constants;
using LeattyServer.Crypto;
using LeattyServer.Data;
using LeattyServer.Helpers;
using LeattyServer.ServerInfo;
using LeattyServer.ServerInfo.Commands;
using LeattyServer.ServerInfo.Player;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LeattyServer;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using LeattyServer.Migrations;
using LeattyServer.ServerInfo.Player.ResourceSystems;
using System.Web.Http.SelfHost;
using System.Web.Http;
using LeattyServer.Data.WZ;
using LeattyServer.ServerInfo.BuddyList;
using LeattyServer.ServerInfo.Packets;
using LeattyServer.ServerInfo.Packets.Handlers;
using LeattyServer.ServerInfo.Map;

namespace LeattyServer
{
    class Program
    {
        public static Dictionary<string, MapleClient> Clients = new Dictionary<string, MapleClient>();
        private static readonly List<Server> LoginServers = new List<Server>();
        public static Dictionary<byte, ChannelServer> ChannelServers = new Dictionary<byte, ChannelServer>();

        //private static AuthServer CustomLoginServer = new AuthServer();
        //private static Server AuthServer = new Server();

        private static Server CashShopServer = new Server();

        private static readonly ExpiringDictionary<int, MigrationData> MigrationQueue = new ExpiringDictionary<int, MigrationData>(new TimeSpan(0, 2, 0));
        private static readonly object MigrationLock = new object();
        //Anti TCP flood
        private static readonly ExpiringDictionary<string, int> ConnectionCount = new ExpiringDictionary<string, int>(new TimeSpan(0, 0, 10));
        private static readonly ExpiringDictionary<string, bool> TempBanList = new ExpiringDictionary<string, bool>(new TimeSpan(0, 10, 0));
        private const int FLOOD_MAX = 20; //20 connections in < 10 seconds equals a 10 min ban, maybe later we will add a perm ban

        private static Dictionary<int, MapleEvent> Events = new Dictionary<int, MapleEvent>();
        private static AutoIncrement EventId = new AutoIncrement();

        private static System.Timers.Timer PingTimer = new System.Timers.Timer(30000);
        private static DateTime LastPing = DateTime.UtcNow;

        public static List<Pair<int, WzMap.Npc>> CustomNpcs { get; private set; }

        private static HttpSelfHostServer dataApi;

        static void Main(string[] args)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.Title = "Leatty " + ServerConstants.Version;

            ServerConstants.LoadFromFile();

            InitializeDatabase();
            LoadDataBuffers();
            CustomNpcs = DataProvider.LoadCustomNpcs();
            StartCashShop();
            StartLogin();
            StartChannels();
            PingTimer.Elapsed += (sender, e) => PingClients();


            StartDataApi();

            GC.Collect();
            string line = string.Empty;
            while (!(line = Console.ReadLine().ToLower()).Contains("stop") && !line.Contains("exit"))
            {
                string[] split = line.Split(' ');
                if (split.Length > 0)
                {
                    switch (split[0])
                    {
                        case "login":
                        if (ServerConstants.LocalHost && Program.Clients.Any())
                        {
                            MapleClient c = Program.Clients.First().Value;
                            MapleAccount acc = MapleAccount.GetAccountFromDatabase("Nuclear");
                            c.SendPacket(LoginAccountHandler.LoginAccountSuccess(acc));
                        }
                        break;
                        case "mapleshark":
                        case "maplesharkconfig":
                        MapleSharkConfigCreator.GenerateConfigFile();
                        break;
                        case "dumpskills":
                        Functions.DumpSkillConstants();
                        break;
                        case "dumpitems":
                        Functions.DumpItems();
                        break;
                        case "send":
                        if (split.Length > 4)
                        {
                            MapleCharacter chr = GetCharacterByName(split[1]);
                            if (chr == null) ServerConsole.Info("Player " + split[1] + "not found");
                            else
                            {
                                PacketWriter pw = new PacketWriter();
                                pw.WriteHexString(split.Fuse(2));
                                chr.Client.SendPacket(pw);
                            }
                        }
                        break;
                        case "notice":
                        if (split.Length > 1)
                        {
                            string message = "[Notice] " + split.Fuse(1);
                            BroadCastWorldPacket(MapleCharacter.ServerNotice(message, 6));
                        }
                        break;
                        default:
                        ServerConsole.Info("Unknown command: " + line);
                        break;
                    }
                }
            }
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Shutting down...");
            Quit();
        }

        private static void StartDataApi()
        {
            HttpSelfHostConfiguration config = new HttpSelfHostConfiguration(ServerConstants.dataApiUrl);

            config.Routes.MapHttpRoute(
                "DataApi", "{controller}/{action}/{param}",
                new { param = RouteParameter.Optional });
            dataApi = new HttpSelfHostServer(config);
            //dataApi.OpenAsync().Wait();

            ServerConsole.Info(String.Format("DataApi running on {0}", ServerConstants.dataApiUrl));
        }

        private static void DisabledQuikEdit()
        {
            IntPtr handle = NativeMethods.GetStdHandle(NativeMethods.STD_INPUT_HANDLE);
            uint mode;
            NativeMethods.GetConsoleMode(handle, out mode);
            mode &= ~NativeMethods.ENABLE_EXTENDED_FLAGS;
            mode &= ~NativeMethods.ENABLE_MOUSE_INPUT;
            NativeMethods.SetConsoleMode(handle, mode);
        }

        public static void StartLogin()
        {
            ushort loginPort = ServerConstants.LoginPort;
            byte[] loginIp;
            if (ServerConstants.LocalHost)
                loginIp = ServerConstants.LocalHostIp;
            else
                loginIp = new byte[] { 0, 0, 0, 0 };

            int i = 1;
            Console.ForegroundColor = ConsoleColor.White;
            Server server = new Server();
            server.OnClientConnected += MapleClientConnect;
            server.Start(new IPAddress(loginIp), loginPort);
            LoginServers.Add(server);
            ServerConsole.Info(String.Format("LoginServer[{0}] is running", i));
            server = new Server();
            server.Start(new IPAddress(loginIp), (ushort)(loginPort + 1)); //server for pinging, we dont rly want it to log everything
            i++;
        }

        public static void InitializeDatabase()
        {
            Database.SetInitializer(new MigrateDatabaseToLatestVersion<LeattyContext, Configuration>());
            using (LeattyContext dbContext = new LeattyContext())
            {
                try
                {
                    dbContext.Database.Initialize(true);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }
            ServerInfo.Guild.MapleGuild.InitializeGuildDatabase();
            LoadCashShopItems();
        }

        public static void StartChannels()
        {
            ushort channelStartPort = ServerConstants.ChannelStartPort;
            byte i = 0;
            byte channelCount = ServerConstants.Channels;
            while (i < channelCount)
            {
                ChannelServer channelServer = new ChannelServer(i);
                channelServer.OnClientConnected += MapleClientConnect;
                ushort port = (ushort)(channelStartPort + i);
                channelServer.Start(ServerConstants.LocalHost ? new IPAddress(ServerConstants.LocalHostIp) : IPAddress.Any, port);
                ChannelServers.Add((byte)(i), channelServer);
                ServerConsole.Info(String.Format("ChannelServer[{0}] is running on port {1}", i + 1, port));
                i++;
            }
            ServerConsole.Info("Server and channels are running.");
            ServerConsole.Info("To safely stop the server type 'exit'");
        }

        public static void StartCashShop()
        {
            CashShopServer = new Server();
            CashShopServer.OnClientConnected += MapleClientConnect;
            CashShopServer.Start(ServerConstants.LocalHost ? new IPAddress(ServerConstants.LocalHostIp) : IPAddress.Any, (ushort)ServerConstants.CashShopPort);
            ServerConsole.Info("CashShopServer is running");
        }

        private static void MapleClientConnect(Socket client)
        {
            String ip = ((IPEndPoint)client.RemoteEndPoint).Address.ToString();
            if (!AllowConnection(ip))
            {
                client.Shutdown(SocketShutdown.Both);
                client.Close();
                return;
            }

            MapleClient Client = new MapleClient(client);
            try
            {
                ushort channelStartPort = ServerConstants.ChannelStartPort;
                byte channelCount = ServerConstants.Channels;
                byte channelID = 0;
                for (byte i = 0; i < ChannelServers.Count; i++)
                {
                    if (ChannelServers[i].Port == ((IPEndPoint)client.LocalEndPoint).Port)
                    {
                        channelID = (byte)(i);
                    }
                }
                if (((IPEndPoint)client.LocalEndPoint).Port == ServerConstants.CashShopPort)
                {
                    channelID = 0xFF;
                }
                Client.Channel = channelID;
                Client.SendHandshake();
                Clients.Add(ip + Functions.Random(), Client);
            }
            catch (Exception e)
            {
                Client.Disconnect(e.ToString());
            }
        }

        public static bool MigrationExists(int id)
        {
            lock (MigrationLock)
            {
                return MigrationQueue.ContainsKey(id);
            }
        }

        public static void EnqueueMigration(int id, MigrationData data)
        {
            lock (MigrationLock)
            {
                MigrationQueue.Add(id, data);
            }
        }
        /// <summary>
        /// Safely tries to dequeue a migration.
        /// </summary>
        public static MigrationData TryDequeueMigration(int id, long auth, byte channel)
        {
            lock (MigrationLock)
            {
                MigrationData connection = null;
                if (MigrationQueue.TryGetValue(id, out connection))
                {
                    if (connection.ConnectionAuth == auth && connection.ToChannel == channel)
                    {
                        MigrationQueue.Remove(id);
                        return connection;
                    }
                }
            }
            return null;
        }

        public static int RegisterEvent(MapleEvent Event)
        {
            int eventId = EventId.Get;
            Events.Add(eventId, Event);
            return eventId;
        }

        public static MapleEvent GetEventById(int id)
        {
            MapleEvent ret = null;
            Events.TryGetValue(id, out ret);
            return ret;
        }

        public static void UnregisterEvent(int eventId)
        {
            Events.Remove(eventId);
        }        
      
        private static void PingClients()
        {
            TimeSpan LastCheck = DateTime.UtcNow.Subtract(LastPing);
            foreach (MapleClient c in Clients.Values.Where(x => x.Account != null).ToList())
            {
                c.SendPacket(PongHandler.PingPacket());
                if (c.LastPong == DateTime.MinValue)
                {
                    c.LastPong = DateTime.UtcNow;
                }
                else
                {
                    TimeSpan timePassed = DateTime.UtcNow.Subtract(c.LastPong);
                    if (timePassed.TotalSeconds > ServerConstants.PingTimeout + LastCheck.TotalSeconds)
                        c.Disconnect("Ping timeout");
                }
            }
            LastPing = DateTime.UtcNow;
        }

        public static bool IsCharacterOnline(int characterId)
        {
            return GetClientByCharacterId(characterId) != null;
        }

        public static bool IsAccountOnline(int accountId)
        {
            return Clients.Values.FirstOrDefault(client => client.Account != null && client.Account.Id == accountId) != null;
        }

        public static MapleClient GetClientByCharacterId(int chrId)
        {
            return Clients.Values.FirstOrDefault(client => client.Account != null && client.Account.Character != null && client.Account.Character.Id == chrId);
        }

        public static MapleClient GetClientByCharacterName(string ign)
        {
            return Clients.Values.FirstOrDefault(client => client.Account != null && client.Account.Character != null && client.Account.Character.Name.Equals(ign, StringComparison.OrdinalIgnoreCase));
        }

        public static MapleCharacter GetCharacterById(int chrId)
        {
            MapleClient c = Clients.Values.FirstOrDefault(client => client.Account != null && client.Account.Character != null && client.Account.Character.Id == chrId);
            return c != null ? c.Account.Character : null;
        }
        public static MapleClient GetClientByAccountId(int accountId)
        {
            return Clients.Values.FirstOrDefault(client => client.Account != null && client.Account.Character != null && client.Account.Id == accountId);
        }

        public static MapleCharacter GetCharacterByAccountId(int accountId)
        {
            MapleClient c = GetClientByAccountId(accountId);
            return c != null ? c.Account.Character : null;
        }

        public static MapleCharacter GetCharacterByName(string ign)
        {
            MapleClient c = Clients.Values.FirstOrDefault(client => client.Account != null && client.Account.Character != null && client.Account.Character.Name.Equals(ign, StringComparison.OrdinalIgnoreCase));
            return c != null ? c.Account.Character : null;
        }

        public static Dictionary<MapleClient, bool> GetOnlineBuddies(List<int> characterIds, List<int> accountIds)
        {
            Dictionary<MapleClient, bool> onlineBuddies = new Dictionary<MapleClient, bool>();
            foreach (MapleClient c in Clients.Values.Where(x => x.Account?.Character != null))
            {
                if (accountIds.Count == 0 && characterIds.Count == 0)
                    break;
                if (!onlineBuddies.ContainsKey(c))
                {
                    if (accountIds.Contains(c.Account.Id))
                    {
                        accountIds.Remove(c.Account.Id);
                        onlineBuddies.Add(c, true);
                    }
                    else if (characterIds.Contains(c.Account.Character.Id))
                    {
                        characterIds.Remove(c.Account.Character.Id);
                        onlineBuddies.Add(c, false);
                    }
                }
            }
            return onlineBuddies;
        }

        public static void Quit()
        {
            foreach (MapleClient c in Clients.Values.ToArray())
            {
                try
                {
                    c.Disconnect("Server shutting down");
                }
                catch { }
            }
            foreach (Server server in LoginServers)
            {
                server.Dispose();
            }
            foreach (ChannelServer channelServer in ChannelServers.Values)
            {
                channelServer.ShutDownChannel();
            }
            ServerInfo.Guild.MapleGuild.SaveGuildsToDatabase();
        }

        public static void RemoveClient(MapleClient c)
        {
            try
            {
                Clients.Remove(Clients.Single(x => x.Value == c).Key);
            }
            catch { }
        }


        //channelId starts at 1
        public static ChannelServer GetChannelServer(byte channelId)
        {
            ChannelServer ret;
            if (ChannelServers.TryGetValue(channelId, out ret))
            {
                return ret;
            }
            return null;
        }

        public static void BroadCastPacket(PacketWriter pw, int channel) //This also goes to clients that aren't logged into a character yet
        {
            foreach (MapleClient client in Clients.Values.Where(c => c.Channel == channel).ToList().Where(client => client.Account?.Character != null))
            {
                client.SendPacket(pw);
            }
        }

        public static void BroadCastChannelPacket(PacketWriter pw, int channel)
        {
            foreach (MapleClient client in Clients.Values.Where(c => c.Channel == channel).ToList().Where(client => client.Account?.Character != null))
            {
                client.SendPacket(pw);
            }
        }

        public static void BroadCastWorldPacket(PacketWriter pw)
        {
            foreach (MapleClient client in Clients.Values.ToList().Where(client => client.Account?.Character != null))
            {
                client.SendPacket(pw);
            }
        }

        public static void BroadcastStaffPacket(PacketWriter pw)
        {
            foreach (MapleClient client in Clients.Values.Where(x => x.Account.AccountType >= 2).ToList())
            {
                if (client.Account != null && client.Account.Character != null)
                {
                    client.SendPacket(pw);
                }
            }
        }

        public static void LoadMonsterDrops()
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            int count = 0;
            count = DataProvider.LoadMobDrops();
            count += DataProvider.LoadGlobalDrops();
            sw.Stop();
            ServerConsole.Info(String.Format("{0} Monsterdrops loaded in {1} ms", count, (int)sw.ElapsedMilliseconds));
        }

        public static void LoadDataBuffers()
        {
            //DisabledQuikEdit();

            Stopwatch allData = new Stopwatch();
            allData.Start();
            int count = 0;            
            count += DataProvider.LoadEtc(@".\NX\Etc.nx");

            ManualResetEvent[] handles = new ManualResetEvent[6];
            for (int i = 0; i < handles.Count(); i++)
                handles[i] = new ManualResetEvent(false);

            ThreadPool.QueueUserWorkItem(new WaitCallback(LoadMobs), handles[2]);
            ThreadPool.QueueUserWorkItem(new WaitCallback(LoadEquips), handles[0]);
            ThreadPool.QueueUserWorkItem(new WaitCallback(LoadItems), handles[1]);
            ThreadPool.QueueUserWorkItem(new WaitCallback(LoadSkills), handles[3]);
            ThreadPool.QueueUserWorkItem(new WaitCallback(LoadQuests), handles[4]);

            handles[2].WaitOne(); //Wait for mob thread to finish           
            ThreadPool.QueueUserWorkItem(new WaitCallback(LoadMaps), handles[5]); //Map needs mob wz info, so wait until mobs finished

            WaitHandle.WaitAll(handles);
            //Always do strings after the other WZs!           
            count += DataProvider.LoadStrings(@".\NX\String.nx");
            ServerConsole.Info("{0} Strings loaded", count);

            ServerConsole.Info("Finished loading .NX in {0} ms", (int)allData.ElapsedMilliseconds);

            Stopwatch sw = new Stopwatch();
            sw.Start();
            count = DataProvider.LoadScripts();
            ServerConsole.Info("{0} Scripts loaded in {1} ms", count, (int)sw.ElapsedMilliseconds);
            sw.Reset();

            /*
            sw.Start();
            Count = LoadCashShopItems();
            ServerConsole.Info(String.Format("{0} CashShop items loaded in {1} ms", Count, (int)sw.ElapsedMilliseconds));
            sw.Reset();
            */
            sw.Start();
            count = AdminCommands.ReloadCommands();
            count += GMCommands.ReloadCommands();
            count += PlayerCommands.ReloadCommands();
            count += DonorCommands.ReloadCommands();
            ServerConsole.Info("{0} Commands loaded in {1} ms", count, (int)sw.ElapsedMilliseconds);
            sw.Reset();
            LoadMonsterDrops();
            allData.Stop();
            ServerConsole.Info("All data loaded in {0} ms", (int)allData.ElapsedMilliseconds);
            ServerConsole.Info("==============================================");
        }

        public static void LoadEquips(object r)
        {
            Stopwatch sw = Stopwatch.StartNew();
            int count = DataProvider.LoadEquips(@".\NX\Character.nx");
            ServerConsole.Info("{0} Equips loaded in {1} ms", count, sw.ElapsedMilliseconds);
            sw.Stop();
            ((ManualResetEvent)r).Set();
        }

        public static void LoadItems(object r)
        {
            Stopwatch sw = Stopwatch.StartNew();
            int count = DataProvider.LoadItems(@".\NX\Item.nx");
            ServerConsole.Info("{0} Items loaded in {1} ms", count, sw.ElapsedMilliseconds);
            sw.Stop();
            ((ManualResetEvent)r).Set();
        }

        public static void LoadMobs(object r)
        {
            Stopwatch sw = Stopwatch.StartNew();
            int mobSkills = DataProvider.LoadMobSkills(@".\NX\Skill.nx");
            ServerConsole.Info("{0} Mob Skills loaded in {1} ms", mobSkills, sw.ElapsedMilliseconds);
            sw = Stopwatch.StartNew();
            int count = DataProvider.LoadMobs(@".\NX\Mob.nx");
            ServerConsole.Info("{0} Mobs loaded in {1} ms", count, sw.ElapsedMilliseconds);
            sw.Stop();
            ((ManualResetEvent)r).Set();
        }

        public static void LoadSkills(object r)
        {
            Stopwatch sw = Stopwatch.StartNew();
            int count = DataProvider.LoadSkills(@".\NX\Skill.nx");
            ServerConsole.Info("{0} Skills loaded in {1} ms", count, sw.ElapsedMilliseconds);
            sw.Stop();
            ((ManualResetEvent)r).Set();
        }

        public static void LoadMaps(object r)
        {
            Stopwatch sw = Stopwatch.StartNew();
            int count = DataProvider.LoadMaps(@".\NX\Map.nx");
            ServerConsole.Info("{0} Maps loaded in {1} ms", count, sw.ElapsedMilliseconds);
            sw.Stop();
            ((ManualResetEvent)r).Set();
        }

        public static void LoadQuests(object r)
        {
            Stopwatch sw = Stopwatch.StartNew();
            int count = DataProvider.LoadQuests(@".\NX\Quest.nx");
            ServerConsole.Info("{0} Quests loaded in {1} ms", count, sw.ElapsedMilliseconds);
            sw.Stop();
            ((ManualResetEvent)r).Set();
        }      

        public static int LoadCashShopItems() => DataProvider.LoadCashShopItems();

        public static bool AllowConnection(string Ip)
        {
            if (TempBanList.ContainsKey(Ip))
            {
                return false;
            }
            if (ConnectionCount.ContainsKey(Ip))
            {
                ConnectionCount[Ip] = ConnectionCount[Ip] + 1;
                if (ConnectionCount[Ip] >= FLOOD_MAX)
                {
                    ServerConsole.Warning("Detected a TCP flood on " + Ip);
                    TempBanList.Add(Ip, true);
                    return false;
                }
            }
            else
            {
                ConnectionCount.Add(Ip, 1);
            }
            return true;
        }
    }
}
