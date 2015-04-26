using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LeattyServer.DB.Models
{
    [Table("QuickSlotKeyMaps")]
    public class QuickSlotKeyMap
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }       
        public int CharacterId { get; set; }
        public byte Index { get; set; }
        public int Key { get; set; }   
    }
}
