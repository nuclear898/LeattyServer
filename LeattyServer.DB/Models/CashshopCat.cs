using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LeattyServer.DB.Models
{
    [Table("CashshopCats")]
    public class CashshopCat
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int ParentId { get; set; }
        public int CsId { get; set; }

        public byte Type { get; set; }

        [MaxLength(100)]
        public String Name { get; set; }
    }
}
