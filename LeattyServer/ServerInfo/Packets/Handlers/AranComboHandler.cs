using System;
using LeattyServer.Constants;
using LeattyServer.Data;
using LeattyServer.Data.WZ;
using LeattyServer.ServerInfo.Player;
using LeattyServer.ServerInfo.Player.ResourceSystems;

namespace LeattyServer.ServerInfo.Packets.Handlers
{
    public class AranComboHandler
    {
        public static void HandleGain(MapleClient c)
        { 
            MapleCharacter chr = c.Account.Character;
            if (chr.IsAran) 
            {               
                AranSystem resource = (AranSystem)chr.Resource;
                resource.LastComboIncreaseTime = DateTime.UtcNow;
                resource.Combo++;
                switch (resource.Combo)
                {
                    case 10:
                    case 20:
                    case 30:
                    case 40:
                    case 50:
                    case 60:
                    case 70:
                    case 80:
                    case 90:
                    case 100:
                        int stacks = resource.Combo / 10;
                        if (chr.GetSkillLevel(Aran1.COMBO_ABILITY) >= stacks)
                            HandleComboBuff(chr, resource.Combo);
                        break;
                }
                PacketWriter pw = new PacketWriter();
                pw.WriteHeader(SendHeader.ShowAranCombo);
                pw.WriteInt(resource.Combo);
                c.SendPacket(pw);
            }
        }

        public static void HandleDecay(MapleClient c)
        {
            //ServerConsole.Info((DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond).ToString());
            
            MapleCharacter chr = c.Account.Character;
            if (!chr.IsAran)
                return;
            AranSystem resource = (AranSystem)chr.Resource;
            if (resource.Combo > 1)
                resource.Combo--;
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

        public static void HandleComboBuff(MapleCharacter chr, int combo)
        {
            Buff oldbuff = chr.CancelBuffSilent(Aran1.COMBO_ABILITY);
            if (oldbuff != null)
            {
                Buff newbuff = new Buff(Aran1.COMBO_ABILITY, oldbuff.Effect, SkillEffect.MAX_BUFF_TIME_MS, chr);
                newbuff.Stacks = combo;
                chr.GiveBuff(newbuff);
            }
            else
            {
                SkillEffect effect = DataBuffer.GetCharacterSkillById(Aran1.COMBO_ABILITY).GetEffect(1);
                Buff buff = new Buff(Aran1.COMBO_ABILITY, effect, SkillEffect.MAX_BUFF_TIME_MS, chr);
                buff.Stacks = combo;
                chr.GiveBuff(buff);
            }
        }
    }
}
