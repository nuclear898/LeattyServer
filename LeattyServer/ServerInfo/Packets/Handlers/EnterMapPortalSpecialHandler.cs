using LeattyServer.ServerInfo.Player;

namespace LeattyServer.ServerInfo.Packets.Handlers
{
    class EnterMapPortalSpecialHandler
    {
        public static void Handle(MapleClient c, PacketReader pr)
        {
            pr.Skip(1);
            string portalName = pr.ReadMapleString();
            c.Account.Character.Map.EnterPortalSpecial(c, portalName);
        }
    }
}
