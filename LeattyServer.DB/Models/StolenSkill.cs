using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LeattyServer.DB.Models
{
    [Table("StolenSkills")]
    public class StolenSkill
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public int CharacterId { get; set; }
        public int SkillId { get; set; }        
        public bool Chosen { get; set; }
        public byte Index { get; set; }
        /* Indexes:
         * 0 =  job 1 skill 1
         * 1 =  job 1 skill 2
         * 2 =  job 1 skill 3
         * 3 =  job 1 skill 4
         * 
         * 4 =  job 2 skill 1
         * 5 =  job 2 skill 2
         * 6 =  job 2 skill 3
         * 7 =  job 2 skill 4
         * 
         * 8 =  job 3 skill 1
         * 9 =  job 3 skill 2
         * 10 = job 3 skill 3
         * 
         * 11 = job 4 skill 1
         * 12 = job 4 skill 2
         */
    }
}
