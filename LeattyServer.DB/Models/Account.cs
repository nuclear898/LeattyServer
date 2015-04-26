using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LeattyServer.DB.Models
{
    [Table("Accounts")]
    public class Account
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int MaplePoints { get; set; }
        public int NXPrepaid { get; set; }
        public int NXCredit { get; set; }

        /// <summary>
        /// A byte defining the type of account
        /// </summary>
        /// <value>
        /// 3 = Admin, 2 = GM, 1 = Donor, 0 = Normal player
        /// </value>
        public byte AccountType { get; set; }

        [MaxLength(13)]
        public String Name { get; set; }
        [MaxLength(128)]
        public String Password { get; set; }
        [MaxLength(32)]
        public String Key { get; set; }
        [MaxLength(134)]
        public String Pic { get; set; }
        [MaxLength(32)]
        public String PicKey { get; set; }
    }
}
