using LeattyServer.Data;
using LeattyServer.Data.WZ;
using LeattyServer.ServerInfo.Player;

namespace LeattyServer.ServerInfo.Packets.Handlers
{
    class UseSkillHandler
    {
        public static void Handle(MapleClient c, PacketReader pr)
        {
            MapleCharacter chr = c.Account.Character;
            if (!chr.DisableActions()) return;
            int tickCount = pr.ReadInt();
            int skillId = pr.ReadInt();
            byte skillLevel = pr.ReadByte();
            if (skillId != Constants.Cleric.HEAL && skillId != Constants.Bishop.ANGEL_RAY) // Heal is already handled through MagicAttack
                SkillEffect.CheckAndApplySkillEffect(c.Account.Character, skillId, DataBuffer.GetCharacterSkillById(skillId));
            else
                chr.EnableActions();
        }
    }
}
