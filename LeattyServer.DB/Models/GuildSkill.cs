using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LeattyServer.DB.Models
{
    [Table("GuildSkills")]
    public class GuildSkill
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public long Timestamp { get; set; }

        public int GuildId { get; set; }
        public int SkillId { get; set; }

        public short Level { get; set; }

        [MaxLength(13)]
        public String Purchaser { get; set; }
    }
}
