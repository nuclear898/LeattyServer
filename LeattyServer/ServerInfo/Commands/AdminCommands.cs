using LeattyServer.Constants;
using LeattyServer.Data;
using LeattyServer.Data.WZ;
using LeattyServer.Helpers;
using LeattyServer.ServerInfo.Inventory;
using LeattyServer.ServerInfo.Map;
using LeattyServer.ServerInfo.Player;
using LeattyServer.ServerInfo.Quest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeattyServer.ServerInfo.Packets;

namespace LeattyServer.ServerInfo.Commands
{
    static class AdminCommands
    {
        private static Dictionary<string, Delegate> Commands = new Dictionary<string, Delegate>();

        public static int ReloadCommands()
        {
            Commands.Clear();

            Commands.Add("printpackets", new Action<string[], MapleClient>(PrintPackets));
            Commands.Add("reloadcs", new Action<string[], MapleClient>(ReloadCS));
            Commands.Add("receive", new Action<string[], MapleClient>(ReceivePacket));
            Commands.Add("npc", new Action<string[], MapleClient>(SpawnNpc));
            Commands.Add("permanpc", new Action<string[], MapleClient>(SpawnPermaNpc));
            Commands.Add("clearitems", new Action<string[], MapleClient>(RemoveMapItems));
            Commands.Add("reloadbuffs", new Action<string[], MapleClient>(ReloadBuffs));
            Commands.Add("dc", new Action<string[], MapleClient>(Disconnect));
            Commands.Add("adminhelp", new Action<string[], MapleClient>(ShowCommands));
            Commands.Add("updatequest", new Action<string[], MapleClient>(UpdateQuest));
            Commands.Add("removequest", new Action<string[], MapleClient>(RemoveQuest));
            Commands.Add("skillinfo", new Action<string[], MapleClient>(SkillInfo));
            Commands.Add("pot", new Action<string[], MapleClient>(SetPotential));
            Commands.Add("buff", new Action<string[], MapleClient>(GetBuff));
            Commands.Add("rbuff", new Action<string[], MapleClient>(RemoveBuff));
            Commands.OrderBy(x => x.Key);
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
                    ServerConsole.Debug("Error parsing Admin command: " + command + "\r\n" + e.ToString());
                    c.Account.Character.SendBlueMessage("An error occured while processing your command");
                }
                return true;
            }
            return false;
        }

        public static void UpdateQuest(string[] split, MapleClient c)
        {
            string data = split[2];
            ushort questId;
            if (ushort.TryParse(split[1], out questId)) 
            {
                MapleQuest quest = c.Account.Character.GetQuest(questId);
                if (quest != null)
                {
                    quest.Data = data;
                    c.SendPacket(quest.Update());
                }
                else
                {
                    quest = new MapleQuest(DataBuffer.GetQuestById(questId), MapleQuestStatus.InProgress, data);
                    c.Account.Character.AddQuest(quest, questId);
                }
            }
        }
        
        public static void RemoveQuest(string[] split, MapleClient c)
        {            
            ushort questId;
            if (ushort.TryParse(split[1], out questId))
            {                
                c.Account.Character.ForfeitQuest(questId);                
            }
        }

        public static void PrintPackets(string[] split, MapleClient c)
        {
            ServerConstants.PrintPackets = !ServerConstants.PrintPackets;
            c.Account.Character.SendMessage("PrintPackets set to: " + ServerConstants.PrintPackets, 6);
        }

        public static void ReloadCS(string[] split, MapleClient c)
        {
            Program.LoadCashShopItems();
            c.Account.Character.SendBlueMessage("CS items have been reloaded");
        }

        public static void ReceivePacket(string[] split, MapleClient c)
        {
            string packet = split.Fuse(1);
            PacketWriter pw = new PacketWriter();
            pw.WriteHexString(packet);
            c.SendPacket(pw);
            ServerConsole.Info("Player sent packet to himself: " + pw.ToString());
        }

        public static void SpawnPermaNpc(string[] split, MapleClient c)
        {
            int npcId = int.Parse(split[1]);
            if (DataBuffer.GetNPCNameById(npcId).Length > 0) //Npc exists
            {
                int mapId = c.Account.Character.Map.MapId;
                WzMap.Npc newNpc = null;
                foreach (ChannelServer channelServer in Program.ChannelServers.Values)
                {
                    if (newNpc == null)
                       newNpc = channelServer.GetMap(mapId).SpawnNpcOnGroundBelow(npcId, c.Account.Character.Position);
                    else
                    channelServer.GetMap(mapId).SpawnNpc(newNpc);
                }
                if (newNpc != null)
                {
                    Program.CustomNpcs.Add(new Pair<int, WzMap.Npc>(mapId, newNpc));
                    DataProvider.SaveCustomNpcs(Program.CustomNpcs);
                }
            }
        }

        public static void SpawnNpc(string[] split, MapleClient c)
        {
            int npcId = int.Parse(split[1]);
            if (DataBuffer.GetNPCNameById(npcId).Length > 0) //Npc exists
            {
                c.Account.Character.Map.SpawnNpcOnGroundBelow(npcId, c.Account.Character.Position);
            }   
        }

        public static void RemoveMapItems(string[] split, MapleClient c)
        {
            MapleMap map = c.Account.Character.Map;
            map.RemoveAllMapItems();
        }

        public static void ReloadBuffs(string[] split, MapleClient c)
        {
            c.Account.Character.SendBlueMessage("Reloading buffs...");
            foreach (var skillKVP in DataBuffer.CharacterSkillBuffer)
            {
                foreach (var effectKVP in skillKVP.Value.SkillEffects)
                {
                    if (skillKVP.Value.IsBuff || effectKVP.Value.Info.ContainsKey(CharacterSkillStat.time))
                    {
                        effectKVP.Value.LoadBuffStats();
                        if (!skillKVP.Value.IsBuff && effectKVP.Value.BuffInfo.Count > 0)
                            skillKVP.Value.IsBuff = true;
                    }
                }  
                
            }
            c.Account.Character.SendBlueMessage("Done!");
        }

        public static void Disconnect(string[] split, MapleClient c)
        {
            string playerName = split[1];
            MapleClient targetClient = Program.GetClientByCharacterName(playerName);
            if (targetClient != null)
            {
                targetClient.Disconnect("Disconnected by chat command from {0}", c.Account.Character.Name);
                c.Account.Character.SendBlueMessage("Succesfully disconnected " + playerName);
            }
            else
                c.Account.Character.SendBlueMessage("Player " + playerName + " is not online");
        }

        public static void SkillInfo(string[] split, MapleClient c)
        {
            int skill = int.Parse(split[1]);
            byte level = byte.Parse(split[2]);
            WzCharacterSkill wzSkill = DataBuffer.GetCharacterSkillById(skill);
            SkillEffect effect = wzSkill.GetEffect(level);
            c.Account.Character.SendBlueMessage(string.Format("Skill {0} level {1} info:", skill, level));
            foreach (var kvp in effect.Info)
            {
                c.Account.Character.SendBlueMessage(string.Format("{0} - {1}", Enum.GetName(typeof(CharacterSkillStat), kvp.Key), kvp.Value));
            }
        }

        public static void SetPotential(string[] split, MapleClient c)
        {
            int pot = int.Parse(split[1]);
            MapleEquip equip = (MapleEquip) c.Account.Character.Inventory.GetEquippedItem(-11);
            if (equip == null) return;
            switch (pot)
            {
                case 0:
                equip.PotentialState = (MaplePotentialState)byte.Parse(split[2]);
                break;
                case 1:
                equip.Potential1 = ushort.Parse(split[2]);
                break;
                case 2:
                equip.Potential2 = ushort.Parse(split[2]);
                break;
                case 3:
                equip.Potential3 = ushort.Parse(split[2]);
                break;
                case 4:
                equip.BonusPotential1 = ushort.Parse(split[2]);
                break;
                case 5:
                equip.BonusPotential2 = ushort.Parse(split[2]);
                break;
            }
            c.SendPacket(MapleInventory.Packets.AddItem(equip, MapleInventoryType.Equip, -11));
        }

        public static void GetBuff(string[] split, MapleClient c)
        {
            int buffBit = int.Parse(split[1]);
            int value = int.Parse(split[2]);
            bool stacking = split.Length > 3 && split[3] == "1";
            BuffStat buffstat = new BuffStat(buffBit, false, stacking);
            c.SendPacket(Buff.GiveBuffTestPacket(buffstat, value));
        }

        public static void RemoveBuff(string[] split, MapleClient c)
        {
            int buffBit = int.Parse(split[1]);            
            BuffStat buffstat = new BuffStat(buffBit);
            c.SendPacket(Buff.RemoveBuffTestPacket(buffstat));
        }

        public static void ShowCommands(string[] split, MapleClient c)
        {
            MapleCharacter chr = c.Account.Character;
            chr.SendBlueMessage("Admin Commands:");
            foreach (string str in Commands.Keys)
            {
                chr.SendBlueMessage("!" + str);
            }
            
            GMCommands.ShowCommands(new string[] {}, c);
        }
    }
}
