using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LeattyServer.DB.Models
{
    [Table("Skills")]
    public class Skill
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public long Expiration { get; set; }
        public int CharacterId { get; set; }
        public int SkillId { get; set; }        
        public byte Level { get; set; }
        public byte MasterLevel { get; set; }
        public short SkillExp { get; set; }
    }
}
