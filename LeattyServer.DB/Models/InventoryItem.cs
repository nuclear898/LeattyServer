using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LeattyServer.DB.Models
{
    [Table("InventoryItems")]
    public class InventoryItem
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        public int ItemId { get; set; }
        public int CharacterId { get; set; }
        public short Position { get; set; }
        public short Quantity { get; set; }
        public short Flags { get; set; }
        [MaxLength(13)]
        public string Creator { get; set; }
        public string Source { get; set; }
    }
}
