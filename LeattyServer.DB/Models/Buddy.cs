using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LeattyServer.DB.Models
{  
    [Table("Buddies")]
    public class Buddy
    {
        [Key]
        [DatabaseGeneratedAttribute(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public int CharacterId { get; set; }
        public int BuddyCharacterId { get; set; }
        public int AccountId { get; set; }
        public int BuddyAccountId { get; set; }
        public bool IsRequest { get; set; }

        [MaxLength(13)]
        public string Name { get; set; }
        [MaxLength(16)]
        public string Group { get; set; }
        [MaxLength(256)]
        public string Memo { get; set; }
    }
}
