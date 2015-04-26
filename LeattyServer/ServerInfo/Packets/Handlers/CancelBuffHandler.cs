using LeattyServer.ServerInfo.Player;

namespace LeattyServer.ServerInfo.Packets.Handlers
{
    class CancelBuffHandler
    {
        public static void Handle(MapleClient c, PacketReader pr)
        {
            int skillId = pr.ReadInt();            
            c.Account.Character.CancelBuff(skillId);            
        }
    }
}
