using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LeattyServer.DB.Models
{
    [Table("SkillCooldowns")]
    public class SkillCooldown
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public long StartTime { get; set; }

        //Note: length was being cast to an int, used to be a long, correct if mistaken
        public int Length { get; set; }
        public int CharacterId { get; set; }
        public int SkillId { get; set; }
    }
}
