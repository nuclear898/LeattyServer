using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LeattyServer.DB.Models
{
    [Table("QuestStatus")]
    public class QuestStatus
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int CharacterId { get; set; }
        public int Quest { get; set; }
        public uint CompleteTime { get; set; }

        public byte Status { get; set; }

        [MaxLength(0xFF)]
        public String CustomData { get; set; }
    }
}
