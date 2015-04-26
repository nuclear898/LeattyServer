using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LeattyServer.DB.Models
{
    [Table("KeyMaps")]
    public class KeyMap
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int CharacterId { get; set; }
        public int Action { get; set; }

        public byte Key { get; set; }
        public byte Type { get; set; }
    }
}
