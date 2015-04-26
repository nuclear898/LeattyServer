using LeattyServer.Constants;
using LeattyServer.Data;
using LeattyServer.Data.WZ;
using LeattyServer.Helpers;
using LeattyServer.ServerInfo.Inventory;
using LeattyServer.ServerInfo.Map;
using LeattyServer.ServerInfo.Map.Monster;
using LeattyServer.ServerInfo.Player;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeattyServer.Data.Scripts;

namespace LeattyServer.ServerInfo.Commands
{
    public static class GMCommands
    {
        private static Dictionary<string, Delegate> Commands = new Dictionary<string, Delegate>();

        public static int ReloadCommands()
        {
            Commands.Clear();

            Commands.Add("hide", new Action<string[], MapleClient>(Hide));
            Commands.Add("spawn", new Action<string[], MapleClient>(SpawnMob));
            Commands.Add("levelup", new Action<string[], MapleClient>(LevelUp));
            Commands.Add("gainexp", new Action<string[], MapleClient>(GainExp));
            Commands.Add("job", new Action<string[], MapleClient>(ChangeJob));
            Commands.Add("find", new Action<string[], MapleClient>(Find));
            Commands.Add("warp", new Action<string[], MapleClient>(Warp));
            Commands.Add("getitem", new Action<string[], MapleClient>(GetItem));
            Commands.Add("drop", new Action<string[], MapleClient>(DropItem));
            Commands.Add("dropmeso", new Action<string[], MapleClient>(DropMesos));
            Commands.Add("level", new Action<string[], MapleClient>(SetLevel));
            Commands.Add("clearskills", new Action<string[], MapleClient>(ClearSkills));
            Commands.Add("clearinv", new Action<string[], MapleClient>(ClearInventory));
            Commands.Add("maxskill", new Action<string[], MapleClient>(MaxSkill));
            Commands.Add("ap", new Action<string[], MapleClient>(SetAP));
            Commands.Add("sp", new Action<string[], MapleClient>(SetSP));
            Commands.Add("reloadnpc", new Action<string[], MapleClient>(ReloadNPCs));
            Commands.Add("online", new Action<string[], MapleClient>(OnlinePlayers));
            Commands.Add("die", new Action<string[], MapleClient>(Die));
            Commands.Add("low", new Action<string[], MapleClient>(Low));
            Commands.Add("heal", new Action<string[], MapleClient>(Heal));
            Commands.Add("offencelist", new Action<string[], MapleClient>(ListOffences));
            Commands.Add("cheaters", new Action<string[], MapleClient>(ListOffences));
            Commands.Add("testoffence", new Action<string[], MapleClient>(TestOffence));
            Commands.Add("help", new Action<string[], MapleClient>(ShowCommands));
            Commands.Add("event", new Action<string[], MapleClient>(StartEvent));
            Commands.Add("killall", new Action<string[], MapleClient>(KillAll));
            Commands.Add("setstats", new Action<string[], MapleClient>(SetStats));
            Commands.Add("removedoors", new Action<string[], MapleClient>(RemoveDoors));
            Commands.Add("dc", new Action<string[], MapleClient>(DisconnectPlayer));
            Commands.Add("say", new Action<string[], MapleClient>(Say));
            Commands.Add("notice", new Action<string[], MapleClient>(Notice));  
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
                    ServerConsole.Debug("Error parsing GMCommand command: " + command + "\r\n" + e.ToString());
                    c.Account.Character.SendBlueMessage("An error occured while processing your command");
                }
                return true;
            }
            return false;
        }

        public static void Template(string[] split, MapleClient c)
        {
            MapleCharacter chr = c.Account.Character;
        }

        public static void Hide(string[] split, MapleClient c)
        {
            MapleCharacter chr = c.Account.Character;
            chr.Hidden = !chr.Hidden;
            if (chr.Hidden)
                chr.Map.HideCharacter(chr);
            else
                chr.Map.UnhideCharacter(chr);
            chr.SendBlueMessage("New hidden status: " + chr.Hidden);
        }

        //!spawnmob [mobId] optional:[mobAmount]
        public static void SpawnMob(string[] split, MapleClient c)
        {
            int mobId = int.Parse(split[1]);
            WzMob mobInfo = DataBuffer.GetMobById(mobId);
            if (mobInfo == null)
                return;
            int amount = 1;
            if (split.Length > 2)
                amount = int.Parse(split[2]);
            if (amount > 1)
            {
                List<MapleMonster> mobs = new List<MapleMonster>();
                for (int i = 0; i < amount; i++)
                {
                    MapleMonster mob = new MapleMonster(mobInfo, c.Account.Character.Map);

                    mobs.Add(mob);
                }
                c.Account.Character.Map.SpawnMobsOnGroundBelow(mobs, new Point(c.Account.Character.Position.X, c.Account.Character.Position.Y - 1));

            }
            else
            {
                MapleMonster mob = new MapleMonster(mobInfo, c.Account.Character.Map);

                c.Account.Character.Map.SpawnMobOnGroundBelow(mob, new Point(c.Account.Character.Position.X, c.Account.Character.Position.Y - 1));
            }
        }

        public static void GainExp(string[] split, MapleClient c)
        {
            MapleCharacter chr = c.Account.Character;
            if (split.Length > 1)
            {
                int exp = int.Parse(split[1]);
                if (exp > 0)
                {
                    chr.GainExp(exp);
                }
            }
        }

        public static void LevelUp(string[] split, MapleClient c)
        {
            MapleCharacter chr = c.Account.Character;
            int times = 1;
            if (split.Length > 1)
            {
                if (int.TryParse(split[1], out times))
                {
                    for (int i = 0; i < times; i++)
                    {
                        chr.LevelUp();
                    }
                }
            }
            else
            {
                chr.LevelUp();
            }
        }

        public static void ReloadNPCs(string[] split, MapleClient c)
        {
            int count = DataProvider.LoadScripts();
            c.Account.Character.SendBlueMessage(count + " NPC scripts have been reloaded");
        }

        public static void ChangeJob(string[] split, MapleClient c)
        {
            MapleCharacter chr = c.Account.Character;
            if (split.Length > 1)
            {
                short job = short.Parse(split[1]);
                chr.ChangeJob(job);
            }
        }

        public static void Warp(string[] split, MapleClient c)
        {
            int mapId;
            if (int.TryParse(split[1], out mapId))
            {
                MapleMap map = Program.GetChannelServer(c.Channel).GetMap(mapId);
                if (map != null)
                {
                    c.Account.Character.ChangeMap(map);
                }
            }
            else
            {
                MapleCharacter victim = Program.GetCharacterByName(split[1]);
                if (victim != null)
                {
                    if (victim.Client.Channel != c.Channel)
                    {
                        c.Account.Character.SendBlueMessage("You are not on the same channel, please move to channel " + victim.Client.Channel);
                    }
                    else
                    {
                        c.Account.Character.ChangeMap(victim.Map, victim.Map.GetClosestSpawnPoint(victim.Position).Name);
                    }
                }
                else
                {
                    c.Account.Character.SendBlueMessage("Player " + split[1] + " not found");
                }             
            }
        }

        public static void DisconnectPlayer(string[] split, MapleClient c)
        {
            string name = split[1];
            MapleCharacter victim = Program.GetCharacterByName(name);
            if (victim != null && victim.Client.Account.AccountType < c.Account.AccountType)
            {
                victim.Client.Disconnect("GM Command by {0}", c.Account.Character);
                c.Account.Character.SendBlueMessage("Succesfully disconnected " + name);
            }
            else
            {
                c.Account.Character.SendBlueMessage("Player is not online or your GM level is not high enough");
            }
        }

        public static void SetLevel(string[] split, MapleClient c)
        {
            MapleCharacter chr = c.Account.Character;
            if (chr == null)
                return;
            byte newLevel;
            if (byte.TryParse(split[1], out newLevel))
            {
                chr.Level = newLevel;
                MapleCharacter.UpdateSingleStat(c, MapleCharacterStat.Level, newLevel);
            }
        }

        public static void Find(string[] split, MapleClient c)
        {
            MapleCharacter chr = c.Account.Character;
            if (chr == null)
                return;
            if (split.Length < 3)
            {
                chr.SendBlueMessage("This command requires 2 arguments: [type] [name]");
                return;
            }
            switch (split[1].ToLower())
            {
                case "item":
                    var itemResults = DataBuffer.GetItemsByName(split.Fuse(2));
                    chr.SendBlueMessage(String.Format("Found {0} matches:", itemResults.Count));
                    if (itemResults.Count > 50)
                        chr.SendBlueMessage("Too many results, please try a more specific search.");
                    else
                    {
                        foreach (var pair in itemResults)
                            chr.SendBlueMessage(String.Format("{0} : {1}", pair.Left, pair.Right));
                    }
                    break;
                case "mob":
                    var mobResults = DataBuffer.GetMobsByName(split.Fuse(2));
                    chr.SendBlueMessage(String.Format("Found {0} matches:", mobResults.Count));
                    if (mobResults.Count > 50)
                        chr.SendBlueMessage("Too many results, please try a more specific search.");
                    else
                    {
                        foreach (var pair in mobResults)
                            chr.SendBlueMessage(String.Format("{0} : {1}", pair.Left, pair.Right));

                    }
                    break;
                case "skill":
                    var skillResults = DataBuffer.GetSkillsByName(split.Fuse(2)).OrderBy(x => x.Key % 1000);
                    chr.SendBlueMessage(String.Format("Found {0} matches:", skillResults.Count()));
                    if (skillResults.Count() > 50)
                        chr.SendBlueMessage("Too many results, please try a more specific search.");
                    else
                    {
                        foreach (var kvp in skillResults)
                            chr.SendBlueMessage(String.Format("{0} : {1}", kvp.Key, kvp.Value));
                    }
                    break;
                case "map":
                    var mapResults = DataBuffer.GetMapsByName(split.Fuse(2));
                    chr.SendBlueMessage(String.Format("Found {0} matches:", mapResults.Count));
                    if (mapResults.Count > 50)
                        chr.SendBlueMessage("Too many results, please try a more specific search.");
                    else
                    {
                        foreach (var pair in mapResults)
                            chr.SendBlueMessage(String.Format("{0} : {1}", pair.Left, pair.Right));

                    }
                    break;
                case "npc":
                    var npcResults = DataBuffer.GetNPCsByName(split.Fuse(2));
                    chr.SendBlueMessage(String.Format("Found {0} matches:", npcResults.Count));
                    if (npcResults.Count > 50)
                        chr.SendBlueMessage("Too many results, please try a more specific search.");
                    else
                    {
                        foreach (var kvp in npcResults)
                            chr.SendBlueMessage(String.Format("{0} : {1}", kvp.Key, kvp.Value));

                    }
                    break;
                case "job":
                    var jobResults = DataBuffer.GetJobsByName(split.Fuse(2));
                    chr.SendBlueMessage(String.Format("Found {0} matches:", jobResults.Count));
                    if (jobResults.Count > 50)
                        chr.SendBlueMessage("Too many results, please try a more specific search.");
                    else
                    {
                        foreach (var kvp in jobResults)
                            chr.SendBlueMessage(String.Format("{0} : {1}", kvp.Key, kvp.Value));

                    }
                    break;
                default:
                    chr.SendBlueMessage("Invalid type, use: item|mob|map|skill|job|npc");
                    break;
            }
        }

        public static void GetItem(string[] split, MapleClient c)
        {
            int itemId;
            if (int.TryParse(split[1], out itemId))
            {
                short quantity;
                if (split.Length > 2)
                {
                    if (!short.TryParse(split[2], out quantity))
                        quantity = 1;
                }
                else
                    quantity = 1;
                MapleItem item = MapleItemCreator.CreateItem(itemId, "!getitem by " + c.Account.Character.Name, quantity);
                if (item == null)
                    c.Account.Character.SendBlueMessage(String.Format("This item does not exist: {0}", itemId));
                else
                    c.Account.Character.Inventory.AddItem(item, item.InventoryType, true);
            }
        }

        public static void DropItem(string[] split, MapleClient c)
        {
            int itemId;
            if (int.TryParse(split[1], out itemId))
            {
                MapleCharacter chr = c.Account.Character;
                short quantity;
                if (split.Length > 2)
                {
                    if (!short.TryParse(split[2], out quantity))
                        quantity = 1;
                }
                else
                    quantity = 1;
                MapleItem item = MapleItemCreator.CreateItem(itemId, "!dropitem by " + chr.Name, quantity);
                if (item == null)
                    c.Account.Character.SendBlueMessage(String.Format("This item does not exist: {0}", itemId));
                else
                {
                    Point targetPosition = chr.Map.GetDropPositionBelow(new Point(chr.Position.X, chr.Position.Y - 50), chr.Position);
                    chr.Map.SpawnMapItem(item, chr.Position, targetPosition, true, 0, chr);
                }
            }
        }

        public static void Notice(string[] split, MapleClient c)
        {
            string message = "[Notice] " + split.Fuse(1);
            Program.BroadCastWorldPacket(MapleCharacter.ServerNotice(message, 6));
        }

        public static void Say(string[] split, MapleClient c)
        {
            string message = "[GM]" + c.Account.Character.Name + ": " + split.Fuse(1);
            Program.BroadCastWorldPacket(MapleCharacter.ServerNotice(message, 6));
        }

        public static void DropMesos(string[] split, MapleClient c)
        {
            int amount;
            if (int.TryParse(split[1], out amount))
            {
                if (amount > 0)
                {
                    MapleCharacter chr = c.Account.Character;
                    Point targetPosition = chr.Map.GetDropPositionBelow(new Point(chr.Position.X, chr.Position.Y - 50), chr.Position);
                    chr.Map.SpawnMesoMapItem(amount, chr.Position, targetPosition, true, MapleDropType.Unk, chr);
                }
            }
        }
        public static void ClearSkills(string[] split, MapleClient c)
        {
            c.Account.Character.ClearSkills();
            c.Account.Character.FakeRelog();
        }

        public static void MaxSkill(string[] split, MapleClient c)
        {
            int skill = int.Parse(split[1]);
            WzCharacterSkill wzSkill = DataBuffer.GetCharacterSkillById(skill);
            if (wzSkill != null)
            {
                c.Account.Character.SetSkillLevel(skill, wzSkill.MaxLevel, wzSkill.MaxLevel, true);
            }
        }

        public static void SetSP(string[] split, MapleClient c)
        {
            int sp = int.Parse(split[1]);
            if (sp < 0)
                return;
            if (sp > short.MaxValue)
                sp = short.MaxValue;
            int table = split.Length > 2 ? int.Parse(split[2]) : 0;
            c.Account.Character.SpTable[table] = sp;
            MapleCharacter.UpdateSingleStat(c, MapleCharacterStat.Sp, c.Account.Character.SpTable[0], false);
        }

        public static void SetAP(string[] split, MapleClient c)
        {
            int ap = int.Parse(split[1]);
            if (ap < 0)
                return;
            if (ap > short.MaxValue)
                ap = short.MaxValue;
            c.Account.Character.AP = (short)ap;
            MapleCharacter.UpdateSingleStat(c, MapleCharacterStat.Ap, ap, false);
        }

        public static void SetStats(string[] split, MapleClient c)
        {
            int stats = int.Parse(split[1]);
            if (stats < 0)
                return;
            if (stats > short.MaxValue)
                stats = short.MaxValue;
            MapleCharacter chr = c.Account.Character;
            chr.Str = (short)stats;
            chr.Dex = (short)stats;
            chr.Int = (short)stats;
            chr.Luk = (short)stats;
            SortedDictionary<MapleCharacterStat, long> statsUpdate = new SortedDictionary<MapleCharacterStat, long>();
            statsUpdate.Add(MapleCharacterStat.Str, stats);
            statsUpdate.Add(MapleCharacterStat.Dex, stats);
            statsUpdate.Add(MapleCharacterStat.Int, stats);
            statsUpdate.Add(MapleCharacterStat.Luk, stats);
            MapleCharacter.UpdateStats(c, statsUpdate, false);
        }

        public static void ClearInventory(string[] split, MapleClient c)
        {
            if (split.Length < 2)
            {
                c.Account.Character.SendBlueMessage("clearinv usage: !clearinv [equip/use/setup/etc/cash]");
                return;
            }
            MapleInventoryType invType;
            switch (split[1])
            {
                case "equipped":
                    invType = MapleInventoryType.Equipped;
                    break;
                case "eq":
                    invType = MapleInventoryType.Equip;
                    break;
                case "use":
                    invType = MapleInventoryType.Use;
                    break;
                case "setup":
                    invType = MapleInventoryType.Setup;
                    break;
                case "etc":
                    invType = MapleInventoryType.Etc;
                    break;
                case "cash":
                    invType = MapleInventoryType.Cash;
                    break;
                default:
                    c.Account.Character.SendBlueMessage("clearinv usage: !clearinv [equipped/eq/use/setup/etc/cash]");
                    return;
            }
            c.Account.Character.Inventory.ClearInventory(invType, c);
        }

        public static void OnlinePlayers(string[] split, MapleClient c)
        {
            Dictionary<int, List<string>> playersPerChannel = new Dictionary<int, List<string>>();
            foreach (MapleClient client in Program.Clients.Values.ToList())
            {
                if (client.Account?.Character != null)
                {
                    string name = client.Account.Character.Name;
                    byte channel = client.Channel;

                    List<string> names;
                    if (playersPerChannel.TryGetValue(channel, out names))
                    {
                        names.Add(name);
                    }
                    else
                    {
                        playersPerChannel.Add(channel, new List<string>() { name });
                    }
                }
            }            
            MapleCharacter chr = c.Account.Character;
            chr.SendBlueMessage("Online Players: ");
            int totalPlayers = 0;
            foreach (var ppc in playersPerChannel)
            {
                totalPlayers += ppc.Value.Count;
                string show = String.Format("Channel {0}: {1} -", ppc.Key, totalPlayers);
                foreach (string name in ppc.Value)
                {
                    show += " " + name + " ";
                }
                chr.SendBlueMessage(show);
            }
            chr.SendBlueMessage(String.Format("Total: {0}", totalPlayers));

        }

        public static void KillAll(string[] split, MapleClient c)
        {
            c.Account.Character.Map.KillAllMobs(c.Account.Character);
        }

        public static void Die(string[] split, MapleClient c)
        {
            c.Account.Character.AddHP(-c.Account.Character.Hp);
        }

        public static void Heal(string[] split, MapleClient c)
        {   
            
            c.Account.Character.AddHP(c.Account.Character.Stats.MaxHp, true, true);
            c.Account.Character.AddMP(c.Account.Character.Stats.MaxMp);
        }


        public static void Low(string[] split, MapleClient c)
        {
            MapleCharacter chr = c.Account.Character;
            chr.AddHP((-chr.Hp) + 1);
            chr.AddMP((-chr.Mp) + 1);
        }

        public static void ListOffences(string[] split, MapleClient c)
        {
            foreach (MapleClient Client in Program.Clients.Values.Where(x => x.CheatTracker.TotalOffenceValue() >= 0).OrderBy(x => x.CheatTracker.TotalOffenceValue()))
            {
                if (Client.Account != null)
                {
                    MapleCharacter chr = Client.Account.Character;
                    if (chr != null)
                        c.Account.Character.SendBlueMessage(String.Format("{0}:[{1}] : {2}->{3}", chr.Name, chr.Id, Client.CheatTracker.TotalOffenceValue(), ServerConstants.MaxOffenceValue));
                }
            }
        }

        public static void TestOffence(string[] split, MapleClient c)
        {
            c.CheatTracker.AddOffence(AntiCheat.OffenceType.NoDelay);
        }

        public static void ShowCommands(string[] split, MapleClient c)
        {
            c.Account.Character.SendBlueMessage("GM Commands:");
            foreach (string str in Commands.Keys)
            {
                c.Account.Character.SendBlueMessage("!" + str);
            }
        }

        public static void StartEvent(string[] split, MapleClient c)
        {
            EventEngine Test = new EventEngine(c.Account.Character, "testevent", 100000000, true);
            Test.StartEvent();
        }

        public static void RemoveDoors(string[] split, MapleClient c)
        {
            c.Account.Character.CancelDoor();
        }     
    }
}
