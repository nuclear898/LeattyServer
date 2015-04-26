using System.Collections.Generic;
using System.Drawing;

namespace LeattyServer.ServerInfo.Player
{
    public class AttackInfo
    {
        public int SkillId { get; set; }
        public byte SkillLevel { get; set; }
        public List<AttackPair> TargetDamageList { get; set; }
        public Point Position { get; set; }
        public int Charge { get; set; }
        public short Display  { get; set; }
        public int Attacks { get; set; }
        public int Targets { get; set; }
        public byte AttacksByte { get; set; }
        public byte Speed { get; set; }
        public byte Unk { get; set; }

        public AttackInfo()
        {
            TargetDamageList = new List<AttackPair>();
        }
    }

    public class AttackPair
    {
        public int TargetObjectId { get; set; }
        public Point position { get; set; }
        public List<int> Damage { get; set; }
        public List<bool> Crits { get; set; }
        public AttackPair()
        {
            Damage = new List<int>();
            Crits = new List<bool>();
        }
    }
}
