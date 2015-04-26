using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Timers;
using LeattyServer.Constants;
using LeattyServer.Data;
using LeattyServer.Data.WZ;
using LeattyServer.Helpers;
using LeattyServer.ServerInfo.Inventory;
using LeattyServer.ServerInfo.Map;
using LeattyServer.ServerInfo.Movement;
using LeattyServer.ServerInfo.Player;
using LeattyServer.DB;
using LeattyServer.DB.Models;
using LeattyServer.ServerInfo.Packets;

namespace LeattyServer.ServerInfo.Guild
{
    public class MapleGuild
    {
        private static Dictionary<int, MapleGuild> Guilds = new Dictionary<int, MapleGuild>();

        public int GuildId { get; set; }
        public int LeaderId = 0;
        private int _GP = 0;
        public int GP
        {
            get
            {
                lock (sync)
                {
                    return _GP;
                }
            }
            set
            {
                lock (sync)
                {
                    _GP = value;
                }
            }
        }
        public int Logo = 0;
        public short LogoColor = 0;
        public string Name;
        public string[] RankTitles = new string[] { "Master", "Jr. Master", "Member", "Member", "Member" };
        public int Capacity = 10;
        public int LogoBG = 0;
        public short LogoBGColor = 0;
        public string Notice;
        public int Signature = 0;
        public int Alliance = 0;
        public List<int> Characters = new List<int>();

        private object sync = new object();
        private MapleGuild()
        {
        }
        private static byte GuildLevel(int GP)
        {
            if (GP < 20000)
                return 1;
            else if (GP < 160000)
                return 2;
            else if (GP < 540000)
                return 3;
            else if (GP < 1280000)
                return 4;
            else if (GP < 2500000)
                return 5;
            else if (GP < 4320000)
                return 6;
            else if (GP < 6860000)
                return 7;
            else if (GP < 10240000)
                return 8;
            else
                return 9;
        }
        private static Dictionary<int, MapleGuild> LoadGuilds()
        {
            List<DB.Models.Guild> dbGuilds;
            using (LeattyContext dbContext = new LeattyContext())
            {
                dbGuilds = dbContext.Guilds.ToList();
            }
            Dictionary<int, MapleGuild> ret = new Dictionary<int, MapleGuild>();

            foreach (DB.Models.Guild DbGuild in dbGuilds)
            {
                MapleGuild gld = new MapleGuild();
                gld.GuildId = DbGuild.Id;
                gld.LeaderId = DbGuild.Leader;
                gld.GP = DbGuild.GP;
                gld.Logo = DbGuild.Logo;
                gld.LogoColor = DbGuild.LogoColor;
                gld.Name = DbGuild.Name;
                gld.RankTitles[0] = DbGuild.Rank1Title;
                gld.RankTitles[1] = DbGuild.Rank2Title;
                gld.RankTitles[2] = DbGuild.Rank3Title;
                gld.RankTitles[3] = DbGuild.Rank4Title;
                gld.RankTitles[4] = DbGuild.Rank5Title;
                gld.Capacity = DbGuild.Capacity;
                gld.LogoBG = DbGuild.LogoBG;
                gld.LogoBGColor = DbGuild.LogoBGColor;
                gld.Notice = DbGuild.Notice;
                gld.Signature = DbGuild.Signature;
                gld.Alliance = DbGuild.AllianceId;

                ret.Add(gld.GuildId, gld);
            }
            return ret;
        }
        public static void InitializeGuildDatabase()
        {
            Guilds = LoadGuilds();
        }
        public static MapleGuild FindGuild(int ID)
        {
            MapleGuild ret = null;
            if (Guilds.TryGetValue(ID, out ret))
                return ret;
            return null;
        }
        private void SendToAllGuildMembers(PacketWriter pw)
        {

            List<Character> DbChars;
            using (LeattyContext DBContext = new LeattyContext())
            {
                DbChars = DBContext.Characters.Where(x => x.GuildId == GuildId).ToList();
            }
            foreach (Character DbChar in DbChars)
            {
                MapleClient c = Program.GetClientByCharacterId(DbChar.Id);
                if (c != null)
                {
                    c.SendPacket(pw);
                }
            }
        }
        public static void SaveGuildsToDatabase()
        {
            foreach (MapleGuild guild in Guilds.Values)
            {
                guild.SaveToDatabase();
            }
        }

        public bool HasCharacter(int characterId) => Characters.Contains(characterId);

        public void AddCharacter(MapleCharacter character)
        {
            character.Guild = this;
            character.GuildRank = 3;
            character.AllianceRank = 5;
            character.GuildContribution = 500;
            MapleCharacter.SaveToDatabase(character);
            UpdateGuildData(character.Client);
        }
        public static MapleGuild CreateGuild(string name, MapleCharacter leader)
        {
            foreach (MapleGuild g in Guilds.Values)
            {
                if (g.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase))
                {
                    return null;
                }
            }
            using (LeattyContext DBContext = new LeattyContext())
            {
                DB.Models.Guild InsertGuild = new DB.Models.Guild();
                InsertGuild.Leader = leader.Id;
                InsertGuild.Name = name;
                DBContext.Guilds.Add(InsertGuild);
                Character DbChar = DBContext.Characters.SingleOrDefault(x => x.Id == leader.Id);
                DbChar.GuildContribution = 500;
                DbChar.AllianceRank = 5;
                DbChar.GuildRank = 1;
                DBContext.Entry<Character>(DbChar).State = System.Data.Entity.EntityState.Modified;
                DBContext.SaveChanges();

                MapleGuild gld = new MapleGuild();
                gld.GuildId = InsertGuild.Id;
                gld.LeaderId = leader.Id;
                gld.GP = 0;
                gld.Logo = 0;
                gld.LogoColor = 0;
                gld.Name = name;
                gld.Capacity = 10;
                gld.LogoBG = 0;
                gld.LogoBGColor = 0;
                gld.Notice = null;
                gld.Signature = 0;
                gld.Alliance = 0;
                Guilds.Add(gld.GuildId, gld);
                return gld;
            }
        }
        public PacketWriter GenerateKickPacket(MapleCharacter character)
        {
            PacketWriter pw = new PacketWriter();
            pw.WriteHeader(SendHeader.GuildData);
            pw.WriteByte(0x35);
            pw.WriteUInt((uint)GuildId);
            pw.WriteInt(character.Id);
            pw.WriteMapleString(character.Name);
            return pw;
        }
        public PacketWriter GenerateSetMaster(MapleCharacter character)
        {
            PacketWriter pw = new PacketWriter();
            pw.WriteHeader(SendHeader.GuildData);
            pw.WriteByte(0x59);
            pw.WriteUInt((uint)GuildId);
            pw.WriteUInt((uint)LeaderId);
            pw.WriteInt(character.Id);
            pw.WriteByte(0);//unk
            return pw;
        }
        public PacketWriter GenerateChangeRankPacket(MapleCharacter character, byte newRank)
        {
            PacketWriter pw = new PacketWriter();
            pw.WriteHeader(SendHeader.GuildData);
            pw.WriteByte(0x46);
            pw.WriteUInt((uint)this.GuildId);
            pw.WriteInt(character.Id);
            pw.WriteByte(newRank);
            return pw;
        }
        public PacketWriter GenerateNoticeChangePacket()
        {
            PacketWriter pw = new PacketWriter();
            pw.WriteHeader(SendHeader.GuildData);
            pw.WriteByte(0x4B);
            pw.WriteUInt((uint)this.GuildId);
            pw.WriteMapleString(Notice);
            return pw;
        }
        public PacketWriter GenerateGuildDisbandPacket()
        {
            PacketWriter pw = new PacketWriter();
            pw.WriteHeader(SendHeader.GuildData);
            pw.WriteByte(0x38);
            pw.WriteUInt((uint)this.GuildId);
            return pw;
        }
        public PacketWriter GenerateGuildInvite(MapleCharacter fromcharacter)
        {
            PacketWriter pw = new PacketWriter();
            pw.WriteHeader(SendHeader.GuildData);
            pw.WriteByte(0x05);
            pw.WriteUInt((uint)this.GuildId);
            pw.WriteMapleString(fromcharacter.Name);
            pw.WriteInt(fromcharacter.Level);
            pw.WriteInt(fromcharacter.Job);
            pw.WriteInt(0);//unknown
            return pw;
        }
        public static void UpdateCharacterGuild(MapleCharacter fromcharacter, string name)
        {
            PacketWriter pw = new PacketWriter();
            pw.WriteHeader(SendHeader.UpdateGuildName);
            pw.WriteInt(fromcharacter.Id);
            pw.WriteMapleString(name);
            fromcharacter.Map.BroadcastPacket(pw, fromcharacter);

        }
        public void BroadcastCharacterJoinedMessage(MapleCharacter fromcharacter)
        {
            PacketWriter pw = new PacketWriter();
            pw.WriteHeader(SendHeader.GuildData);
            pw.WriteByte(0x2D);
            pw.WriteUInt((uint)this.GuildId);
            pw.WriteInt(fromcharacter.Id);
            pw.WriteStaticString(fromcharacter.Name, 13);
            pw.WriteInt((int)fromcharacter.Job);
            pw.WriteInt((int)fromcharacter.Level);
            pw.WriteInt((int)fromcharacter.GuildRank);
            if (Program.IsCharacterOnline((int)fromcharacter.Id))
            {
                pw.WriteInt(1);
            }
            else
            {
                pw.WriteInt(0);
            }
            pw.WriteInt(3);//nCommitment ?? "alliance rank"
            int contribution = (int)fromcharacter.GuildContribution;
            pw.WriteInt(contribution);

            SendToAllGuildMembers(pw);
        }
        public PacketWriter GenerateGuildDataPacket()
        {
            PacketWriter pw = new PacketWriter();
            pw.WriteHeader(SendHeader.GuildData);
            pw.WriteByte(0x20);
            pw.WriteByte(1);//?
            pw.WriteUInt((uint)GuildId);
            pw.WriteMapleString(Name);
            foreach (string ranks in RankTitles)
            {
                pw.WriteMapleString(ranks);
            }
            List<Character> GuildCharacters;
            using (LeattyContext DBContext = new LeattyContext())
            {
                GuildCharacters = DBContext.Characters.Where(x => x.GuildId == GuildId).ToList();
            }
            pw.WriteByte((byte)GuildCharacters.Count);
            foreach (Character Character in GuildCharacters)
            {
                pw.WriteInt(Character.Id);
            }
            lock (sync)
            {
                GP = 0;
                foreach (Character Character in GuildCharacters)
                {
                    MapleClient c = Program.GetClientByCharacterId(Character.Id);
                    int contribution = 0;
                    if (c == null)
                    {
                        pw.WriteStaticString(Character.Name, 13);
                        pw.WriteInt(Character.Job);
                        pw.WriteInt(Character.Level);
                        pw.WriteInt(Character.GuildRank);

                        pw.WriteInt(0);
                        pw.WriteInt(3);//nCommitment ?? "alliance rank"
                        contribution = Character.GuildContribution;
                    }
                    else//the character might have unsaved data so we use this instead.
                    {
                        MapleCharacter ch = c.Account.Character;
                        pw.WriteStaticString(ch.Name, 13);
                        pw.WriteInt(ch.Job);
                        pw.WriteInt(ch.Level);
                        pw.WriteInt(ch.GuildRank);
                        pw.WriteInt(1);
                        pw.WriteInt(3);//nCommitment ?? "alliance rank"
                        contribution = ch.GuildContribution;
                    }
                    GP += contribution;
                    pw.WriteInt(contribution);
                }
            }
            pw.WriteInt(Capacity);
            pw.WriteShort((short)LogoBG);
            pw.WriteByte((byte)LogoBGColor);
            pw.WriteShort((short)Logo);
            pw.WriteByte((byte)LogoColor);
            pw.WriteMapleString(Notice);
            pw.WriteInt(GP);
            pw.WriteInt(GP);//not sure abuot this one it may be something else GP related.
            pw.WriteInt(Alliance);
            pw.WriteByte(GuildLevel(GP));
            List<GuildSkill> DbGuildSkills;
            using (LeattyContext DBContext = new LeattyContext())
            {
                DbGuildSkills = DBContext.GuildSkills.Where(x => x.GuildId == GuildId).ToList();
            }
            pw.WriteShort(0);//unk
            pw.WriteShort((short)DbGuildSkills.Count);

            foreach (GuildSkill DbGuildSkill in DbGuildSkills)
            {
                pw.WriteInt(DbGuildSkill.SkillId);
                pw.WriteShort(DbGuildSkill.Level);
                pw.WriteLong(DbGuildSkill.Timestamp);
                pw.WriteMapleString(DbGuildSkill.Purchaser);
                pw.WriteMapleString("");//activator
            }
            return pw;
        }
        public void SaveToDatabase()
        {
            using (LeattyContext DBContext = new LeattyContext())
            {
                DB.Models.Guild UpdateGuild = DBContext.Guilds.Single(x => x.Id == GuildId);
                UpdateGuild.Id = GuildId;
                UpdateGuild.Leader = LeaderId;
                UpdateGuild.GP = GP;
                UpdateGuild.Logo = Logo;
                UpdateGuild.LogoColor = LogoColor;
                UpdateGuild.LogoBG = LogoBG;
                UpdateGuild.LogoBGColor = LogoBGColor;
                UpdateGuild.Rank1Title = RankTitles[0];
                UpdateGuild.Rank2Title = RankTitles[1];
                UpdateGuild.Rank3Title = RankTitles[2];
                UpdateGuild.Rank4Title = RankTitles[3];
                UpdateGuild.Rank5Title = RankTitles[4];
                UpdateGuild.Capacity = Capacity;
                UpdateGuild.Notice = Notice;
                UpdateGuild.Signature = Signature;
                UpdateGuild.AllianceId = Alliance;
                DBContext.Entry<DB.Models.Guild>(UpdateGuild).State = System.Data.Entity.EntityState.Modified;
                DBContext.SaveChanges();
            }
        }
        public void UpdateGuildData(MapleClient recipient = null)
        {
            if (recipient != null)
            {
                recipient.SendPacket(GenerateGuildDataPacket());
            }
            else
            {
                SendToAllGuildMembers(GenerateGuildDataPacket());
            }
        }
        public void ChangeNotice(string notice)
        {
            Notice = notice;
            SendToAllGuildMembers(GenerateNoticeChangePacket());
        }
        public void Disband()
        {
            List<Character> GuildCharacters;
            using (LeattyContext DBContext = new LeattyContext())
            {
                GuildCharacters = DBContext.Characters.Where(x => x.GuildId == GuildId).ToList();
            }
            Guilds.Remove(this.GuildId);

            foreach (Character character in GuildCharacters)
            {
                MapleClient c = Program.GetClientByCharacterId(character.Id);
                c.Account.Character.Guild = null;
                if (c != null)
                {
                    c.SendPacket(GenerateGuildDisbandPacket());
                    UpdateCharacterGuild(c.Account.Character, "");
                    c.Account.Character.GuildContribution = 0;
                    c.Account.Character.AllianceRank = 5;
                    c.Account.Character.GuildRank = 5;
                    MapleCharacter.SaveToDatabase(c.Account.Character);
                }
            }

        }
        public void RemoveCharacter(MapleCharacter character)
        {
            character.GuildRank = 5;
            character.AllianceRank = 5;
            character.GuildContribution = 0;
            character.Guild = null;

            MapleGuild.UpdateCharacterGuild(character, "");
            character.Client.SendPacket(GenerateGuildDisbandPacket());
            UpdateGuildData();
        }
        public void SetMaster(MapleCharacter character, MapleCharacter oldMaster)
        {
            character.GuildRank = 1;
            oldMaster.GuildRank = 2;
            SendToAllGuildMembers(GenerateSetMaster(character));
            this.LeaderId = character.Id;
        }
        public void ChangeRank(MapleCharacter character, byte guildrank)
        {
            if (guildrank > 1 && guildrank <= 5)
            {
                character.GuildRank = guildrank;
                SendToAllGuildMembers(GenerateChangeRankPacket(character, guildrank));
            }
        }
        public void KickCharacter(MapleCharacter character)
        {
            character.GuildRank = 5;
            character.AllianceRank = 5;
            character.GuildContribution = 0;
            character.Guild = null;

            SendToAllGuildMembers(GenerateKickPacket(character));
            MapleGuild.UpdateCharacterGuild(character, "");
            MapleCharacter.SaveToDatabase(character);
        }
    }
}
