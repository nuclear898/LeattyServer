using LeattyServer.Data.WZ;
using LeattyServer.ServerInfo.Inventory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeattyServer.Scripting;

namespace LeattyServer.Data.Scripts
{
    class ScriptInterface : AbstractScriptInterface
    {
        public override byte GetEquipRequiredLevel(int equipItemId)
        {
            WzEquip equipInfo = DataBuffer.GetEquipById(equipItemId);
            return equipInfo == null ? (byte)0 : equipInfo.ReqLevel;
        }

        public override long GetEquipRevealCost(int equipItemId)
        {
            WzEquip equipInfo = DataBuffer.GetEquipById(equipItemId);
            return equipInfo == null ? 0 : equipInfo.RevealPotentialCost;
        }

        public override string GetPotentialName(int potentialId, byte equipRequiredLevel)
        {
            var potential = DataBuffer.GetPotential(potentialId);          
            byte level = (byte)Math.Round(equipRequiredLevel / 10.0);
            if (level == 0)
                level = 1;
            return potential == null ? string.Empty : potential.GetPotentialText(level);
        }      
    }
}
