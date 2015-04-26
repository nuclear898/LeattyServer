using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LeattyServer.DB.Models
{
    [Table("Characters")]
    public class Character
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]

        public int Id { get; set; }
        public int AccountId { get; set; }
        [MaxLength(13)]
        public String Name { get; set; }
        public byte Level { get; set; }
        public short Job { get; set; }
        public short Str { get; set; }
        public short Dex { get; set; }
        public short Luk { get; set; }
        public short Int { get; set; }
        public int GuildId { get; set; }
        [MaxLength(50)]
        public String Sp { get; set; }
        public long Exp { get; set; }
        public short SubJob { get; set; }
        public long Mesos { get; set; }
        public int MapId { get; set; }
        public byte SpawnPoint { get; set; }
        public int Hp { get; set; }
        public int Mp { get; set; }
        public int MaxHp { get; set; }
        public int MaxMp { get; set; }
        public int Fame { get; set; }
        public int Hair { get; set; }
        public int Face { get; set; }
        public int FaceMark { get; set; }
        public int TamerEars { get; set; }
        public int TamerTail { get; set; }
        public int GuildContribution { get; set; }
        public int Charisma { get; set; }
        public int Insight { get; set; }
        public int Will { get; set; }
        public int Craft { get; set; }
        public int Sense { get; set; }
        public int Charm { get; set; }
        public short AP { get; set; }
        public short BuddyCapacity { get; set; }
        public byte Gender { get; set; }
        public byte Skin { get; set; }
        public byte GuildRank { get; set; }
        public byte AllianceRank { get; set; }
        public byte Fatigue { get; set; }
    }
}
