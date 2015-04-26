using LeattyServer.Constants;
using LeattyServer.Data.WZ;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeattyServer.ServerInfo.Player.ResourceSystems
{
    class AranSystem : ResourceSystem
    {
        public int Combo { get; set; }
        public DateTime LastComboIncreaseTime { get; set; }

        public AranSystem() : base (ResourceSystemType.Aran)
        {
            Combo = 0;
            LastComboIncreaseTime = DateTime.UtcNow;
        }

        public static bool HandleComboUsage(MapleCharacter chr, int comboCon)
        {
            AranSystem resource = (AranSystem)chr.Resource;
            if (resource.Combo < comboCon)
                return false;
            else
            {
                resource.Combo -= comboCon;
                Buff currentComboBuff = chr.GetBuff(Aran1.COMBO_ABILITY);
                if (currentComboBuff != null)
                {
                    if (resource.Combo / 10 < currentComboBuff.Stacks / 10)
                    {
                        Buff newbuff = new Buff(Aran1.COMBO_ABILITY, currentComboBuff.Effect, SkillEffect.MAX_BUFF_TIME_MS, chr);
                        newbuff.Stacks = resource.Combo;
                        chr.GiveBuff(newbuff);
                    }
                }
            }
            return true;
        }
    }
}
