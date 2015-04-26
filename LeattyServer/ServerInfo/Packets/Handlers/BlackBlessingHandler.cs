using LeattyServer.Constants;
using LeattyServer.Data;
using LeattyServer.Data.WZ;
using LeattyServer.ServerInfo.Player;

namespace LeattyServer.ServerInfo.Packets.Handlers
{
    public class BlackBlessingHandler
    {
        public static void Handle(MapleClient c)
        {
            MapleCharacter chr = c.Account.Character;
            if (!chr.IsLuminous)
                return;
            int skillLevel = chr.GetSkillLevel(Luminous2.BLACK_BLESSING);
            if (skillLevel == 0)
                return;
            Buff buff = chr.GetBuff(Luminous2.BLACK_BLESSING);
            int orbAmount = 0;
            if (buff != null)
            {
                buff.CancelRemoveBuffSchedule();
                orbAmount = buff.Stacks;
            }
            if (orbAmount < 3)
                orbAmount += 1;
            if (buff == null)
            {
                buff = new Buff(Luminous2.BLACK_BLESSING, DataBuffer.CharacterSkillBuffer[Luminous2.BLACK_BLESSING].GetEffect(1), SkillEffect.MAX_BUFF_TIME_MS, chr);
                chr.GiveBuff(buff);
            }
            else
            {
                Buff newbuff = new Buff(Luminous2.BLACK_BLESSING, buff.Effect, int.MaxValue, chr);
                newbuff.Stacks = orbAmount;
                chr.GiveBuff(newbuff);
            }
        }
    }
}
