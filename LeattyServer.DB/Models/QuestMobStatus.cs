using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LeattyServer.DB.Models
{
    [Table("QuestStatusMobs")]
    public class QuestMobStatus
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public int QuestStatusId { get; set; }
        public int Mob { get; set; }
        public int Count { get; set; }
    }
}
