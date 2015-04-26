using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeattyServer.ServerInfo.Map;
using LeattyServer.ServerInfo.Packets;
using LeattyServer.ServerInfo.Player;

namespace LeattyServer.ServerInfo.Party
{
    public class MapleParty
    {
        private static Dictionary<int, MapleParty> Parties = new Dictionary<int, MapleParty>();
        private static int GlobalPartyID = 1;
        private static object PartyIdLock = new object();

        public int Id { get; private set; }
        public int LeaderId { get; private set; }
        public string Name { get; set; }
        public bool Private { get; set; }

        private List<int> Characters = new List<int>();
        private Dictionary<int, PartyCharacterInfo> OfflineCharacterCache = new Dictionary<int, PartyCharacterInfo>();

        public MapleParty(string partyName)
        {
            Name = partyName;
            lock (PartyIdLock)
            {
                Id = GlobalPartyID++;
            }
        }

        public void Dispose()
        {
            lock (Parties)
            {
                Parties.Remove(Id);
            }
            OfflineCharacterCache = null;
        }

        //Returns the party the given characterId is currently in
        public static MapleParty FindParty(int characterId)
        {
            //this method just scans each existing party for the chrId. It can become a bit clunky if we have many parties and high traffic,
            //so if we save parties to a database this method should be rewritten to search for the party id.
            lock (Parties)
            {
                foreach (MapleParty party in Parties.Values)
                {
                    if (party.Characters.Contains(characterId))
                        return party;
                }
            }
            return null;
        }

        public static MapleParty CreateParty(MapleCharacter owner, string partyName, bool privateParty)
        {
            MapleParty party = new MapleParty(partyName);
            lock (Parties)
            {
                Parties.Add(party.Id, party);
            }
            party.LeaderId = owner.Id;
            party.Characters.Add(owner.Id);
            party.Private = privateParty;
            owner.Client.SendPacket(Packets.CreateParty(party));
            return party;
        }        

        public void SetLeader(int newLeaderId)
        {
            if (Characters.Contains(newLeaderId))
            {
                LeaderId = newLeaderId;
                for (int i = 0; i < Characters.Count; i++)
                {
                    MapleClient c = Program.GetClientByCharacterId(Characters[i]);
                    if (c != null)
                    {
                        c.SendPacket(Packets.SetLeader(LeaderId, false));
                    }
                }
            }
        }

        public bool CharacterIdIsMember(int characterId) => Characters.Contains(characterId);
     
        public bool AddPlayer(MapleCharacter chr)
        {
            if (Characters.Count < 6 && !Characters.Contains(chr.Id))
            {
                Characters.Add(chr.Id);
                chr.Party = this;
                foreach (int i in Characters)
                {
                    MapleClient c = Program.GetClientByCharacterId(i);
                    if (c != null)
                    {
                        c.SendPacket(Packets.PlayerJoin(this, chr));
                        if (c.Account.Character.Map == chr.Map)
                        {
                            c.SendPacket(Packets.UpdatePartyMemberHp(chr));
                            chr.Client.SendPacket(Packets.UpdatePartyMemberHp(c.Account.Character));
                        }
                    }
                }
                return true;
            }
            return false;
        }

        public PartyCharacterInfo GetPartyCharacterInfo(int characterId)
        {
            MapleCharacter chr = Program.GetCharacterById(characterId);
            if (chr != null) //Player is online
            {
                return new PartyCharacterInfo(chr.Id, chr.Name, chr.Level, chr.Job, chr.SubJob, chr.Client.Channel, chr.MapId);
            }
            else
            {
                PartyCharacterInfo chrInfo;
                lock (OfflineCharacterCache)
                {
                    if (OfflineCharacterCache.TryGetValue(characterId, out chrInfo))
                    {
                        return chrInfo;
                    }
                    else
                    {
                        //Character info is not in the cache so we have to load the chr from DB and then store it
                        chr = MapleCharacter.LoadFromDatabase(characterId, true);
                        if (chr != null)
                        {
                            chrInfo = new PartyCharacterInfo(chr.Id, chr.Name, chr.Level, chr.Job, chr.SubJob);
                            OfflineCharacterCache.Add(characterId, chrInfo);
                            return chrInfo;
                        }
                        else //There was an error loading the character from the DB
                        {
                            return null; 
                        }
                    }
                }
            }
        }

        public bool RemovePlayer(int id, bool kicked)
        {
            if (Characters.Remove(id))
            {
                MapleCharacter chr = Program.GetCharacterById(id);
                if (chr != null)
                {
                    //Player is online
                    chr.Party = null;
                }
                if (Characters.Count > 0)
                {
                    if (id == LeaderId) //Player was the party leader
                    {
                        int newLeaderId = -1;
                        foreach (int i in Characters)
                        {
                            MapleCharacter newLeader = Program.GetCharacterById(i); //online players first
                            if (newLeader != null)
                            {
                                newLeaderId = newLeader.Id;
                                break;
                            }
                        }
                        if (newLeaderId == -1) //No online party members 
                            newLeaderId = Characters[0]; //Then just take the first next character

                        LeaderId = newLeaderId;

                        for (int i = 0; i < Characters.Count; i++)
                        {
                            MapleClient c = Program.GetClientByCharacterId(Characters[i]);
                            if (c != null)
                            {
                                c.SendPacket(Packets.SetLeader(LeaderId, false));
                                c.SendPacket(Packets.PlayerLeave(this, chr, c.Account.Character, false, kicked));
                            }
                        }
                        chr.Client.SendPacket(Packets.PlayerLeave(this, chr, chr, false, kicked));
                    }
                }
                else //no players left, disband
                {
                    Dispose();
                    chr.Client.SendPacket(Packets.PlayerLeave(this, chr, chr, true));
                }
                chr.Party = null;
                return true;
            }
            return false;
        }

        public List<MapleCharacter> GetCharactersOnMap(MapleMap map, int sourceCharacterId = 0)
        {
            List<MapleCharacter> ret = new List<MapleCharacter>();
            foreach (int i in Characters)
            {
                MapleCharacter chr;
                if (i != sourceCharacterId && (chr = map.GetCharacter(i)) != null)
                    ret.Add(chr);
            }
            return ret;
        }

        public void CacheCharacterInfo(MapleCharacter chr)
        {
            if (Characters.Contains(chr.Id))
            {
                lock (OfflineCharacterCache)
                {
                    OfflineCharacterCache.Remove(chr.Id);
                    OfflineCharacterCache.Add(chr.Id, new PartyCharacterInfo(chr.Id, chr.Name, chr.Level, chr.Job, chr.SubJob));
                }
            }
        }

        public void UpdateParty()
        {
            for (int i = 0; i < Characters.Count; i++)
            {
                MapleClient c = Program.GetClientByCharacterId(Characters[i]);
                if (c != null)
                {
                    c.SendPacket(Packets.UpdateParty(this));
                }
            }
        }

        public MapleCharacter GetLeader()
        {
            return Program.GetCharacterById(LeaderId);
        }

        public void BroadcastPacket(PacketWriter packet, int chrIdFrom = 0, bool sendToSource = false)
        {
            foreach (int i in Characters)
            {
                MapleCharacter chr = Program.GetCharacterById(i);
                if (chr != null && (chr.Id != chrIdFrom || sendToSource))
                {
                    chr.Client.SendPacket(packet);
                }
            }
        }

        public static class Packets
        {
            public static PacketWriter GenerateInvite(MapleCharacter from)
            {
                PacketWriter pw = new PacketWriter();
                pw.WriteHeader(SendHeader.PartyInfo);
                pw.WriteByte(0x04);
                pw.WriteInt(from.Id);
                pw.WriteMapleString(from.Name);
                pw.WriteInt(from.Level);
                pw.WriteInt(from.Job);
                pw.WriteInt(from.SubJob); //guessed.
                pw.WriteShort(0);
                return pw;
            }

            public static PacketWriter CreateParty(MapleParty party)
            {
                PacketWriter pw = new PacketWriter();
                pw.WriteHeader(SendHeader.PartyInfo);
                pw.WriteByte(0x10);
                pw.WriteInt(party.Id);
                pw.WriteInt(999999999); //telerock?
                pw.WriteInt(999999999); //telerock?
                pw.WriteInt(0);
                pw.WriteShort(0);
                pw.WriteShort(0);
                pw.WriteByte(0);

                pw.WriteBool(!party.Private);
                pw.WriteMapleString(party.Name);

                return pw;
            }

            public static PacketWriter UpdatePartyName(MapleParty party)
            {
                PacketWriter pw = new PacketWriter(SendHeader.PartyInfo);
                pw.WriteByte(0x4D);
                pw.WriteBool(!party.Private);
                pw.WriteMapleString(party.Name);
                return pw;
            }

            public static PacketWriter InviteResponse(byte response, string ign)
            {
                PacketWriter pw = new PacketWriter();
                pw.WriteHeader(SendHeader.PartyInfo);
                pw.WriteByte(response);
                pw.WriteMapleString(ign);
                return pw;
            }

            private static void AddPartyPlayersInfo(PacketWriter pw, MapleParty party)
            {
                List<PartyCharacterInfo> chrs = new List<PartyCharacterInfo>();
                foreach (int i in party.Characters)
                    chrs.Add(party.GetPartyCharacterInfo(i));

                for (int i = 0; i < 6; i++)
                {
                    pw.WriteInt(i < chrs.Count ? chrs[i].Id : 0);
                }
                for (int i = 0; i < 6; i++)
                {
                    if (i < chrs.Count)
                    {
                        pw.WriteStaticString(chrs[i].Name, 13);
                    }
                    else
                    {
                        pw.WriteZeroBytes(13);
                    }
                }
                for (int i = 0; i < 6; i++)
                {
                    pw.WriteInt(i < chrs.Count ? chrs[i].Job : 0);
                }
                for (int i = 0; i < 6; i++)
                {
                    pw.WriteInt(0);
                }
                for (int i = 0; i < 6; i++)
                {
                    pw.WriteInt(i < chrs.Count ? chrs[i].Level : 0);
                }
                for (int i = 0; i < 6; i++)
                {
                    if (i < chrs.Count)
                    {
                        pw.WriteInt(chrs[i].Channel);
                    }
                    else
                    {
                        pw.WriteInt(-2);
                    }
                }
                for (int i = 0; i < 6; i++)
                {
                    pw.WriteInt(0);
                }
                pw.WriteInt(party.LeaderId); //leader
                for (int i = 0; i < 6; i++)
                {
                    pw.WriteInt(i < chrs.Count ? chrs[i].MapId : 0);
                }
                for (int i = 0; i < 6; i++)
                {
                    if (i < chrs.Count)
                    {
                        //todo: doors
                        //MapleCharacter chr = chrs[i];
                        pw.WriteInt(999999999);
                        pw.WriteInt(999999999);
                        pw.WriteInt(0);
                        pw.WriteInt(-1);
                        pw.WriteInt(-1);
                    }
                    else
                    {
                        pw.WriteZeroBytes(20);
                    }
                }
                pw.WriteBool(!party.Private);
                pw.WriteMapleString(party.Name);
            }

            public static PacketWriter UpdatePartyMemberHp(MapleCharacter chr)
            {
                PacketWriter pw = new PacketWriter();
                pw.WriteHeader(SendHeader.UpdatePartyMemberHp);
                pw.WriteInt(chr.Id);
                pw.WriteInt(chr.Hp);
                pw.WriteInt(chr.MaxHp);
                return pw;
            }

            public static PacketWriter UpdateParty(MapleParty party)
            {
                PacketWriter pw = new PacketWriter();
                pw.WriteHeader(SendHeader.PartyInfo);
                pw.WriteByte(0x0F);
                pw.WriteInt(party.Id);
                AddPartyPlayersInfo(pw, party);
                return pw;
            }

            public static PacketWriter SetLeader(int leader, bool dc)
            {
                PacketWriter pw = new PacketWriter();
                pw.WriteHeader(SendHeader.PartyInfo);
                pw.WriteByte(0x30); //0x2D + 3 ? not sure todo: check this
                pw.WriteInt(leader);
                pw.WriteBool(dc);
                return pw;
            }

            public static PacketWriter PlayerJoin(MapleParty party, MapleCharacter newChar)
            {
                PacketWriter pw = new PacketWriter();
                pw.WriteHeader(SendHeader.PartyInfo);
                pw.WriteByte(0x18); //0x15 +3
                pw.WriteInt(party.Id);
                pw.WriteMapleString(newChar.Name);
                AddPartyPlayersInfo(pw, party);
                return pw;
            }

            public static PacketWriter PlayerLeave(MapleParty party, MapleCharacter leaveChar, MapleCharacter recipient, bool disband, bool kicked = false)
            {
                PacketWriter pw = new PacketWriter();
                pw.WriteHeader(SendHeader.PartyInfo);
                pw.WriteByte(0x15); //0x12 + 3
                pw.WriteInt(party.Id);
                pw.WriteInt(leaveChar.Id);
                pw.WriteBool(!disband);
                if (!disband)
                {
                    pw.WriteBool(kicked);
                    pw.WriteMapleString(leaveChar.Name);
                    AddPartyPlayersInfo(pw, party);
                }
                return pw;
            }
        }
    }
}
