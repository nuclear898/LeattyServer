using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LeattyServer.DB.Models
{
    [Table("CashshopLikes")]
    public class CashshopLike
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int CharacterId { get; set; }
        public int CsId { get; set; }
    }
}
