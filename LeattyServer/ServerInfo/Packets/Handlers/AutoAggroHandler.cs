using LeattyServer.ServerInfo.Map.Monster;
using LeattyServer.ServerInfo.Player;

namespace LeattyServer.ServerInfo.Packets.Handlers
{
    public class AutoAggroHandler
    {
        public static void Handle(MapleClient c, PacketReader pr)
        {
            MapleMonster mob = c.Account.Character.Map.GetMob(pr.ReadInt());
            if (mob == null) return;
            mob.ControllerHasAggro = true;
        }
    }
}
