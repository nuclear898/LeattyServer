using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LeattyServer.DB.Models
{
    [Table("CashshopItems")]
    public class CashshopItem
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int CatId { get; set; }
        public int ItemId { get; set; }
        public int Price { get; set; }
        public int NewPrice { get; set; }
        public int Amount { get; set; }
        public int TimesBought { get; set; }
        public int Order { get; set; }
        public int Likes { get; set; }

        public byte MinLevel { get; set; }
        public byte Special { get; set; }
        public byte Featured { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime DateFrom { get; set; }
        public DateTime DateTo { get; set; }

        [MaxLength(200)]
        public String Image { get; set; }
    }
}
