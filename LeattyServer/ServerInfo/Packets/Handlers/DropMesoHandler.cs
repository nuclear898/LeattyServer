using System.Drawing;
using LeattyServer.ServerInfo.Map;
using LeattyServer.ServerInfo.Player;

namespace LeattyServer.ServerInfo.Packets.Handlers
{
    class DropMesoHandler
    {
        public static void Handle(MapleClient c, PacketReader pw)
        {
            MapleCharacter chr = c.Account.Character;
            if (!chr.DisableActions()) return;
            int tickCount = pw.ReadInt();
            int mesos = pw.ReadInt();
            if (mesos > 50000 || mesos < 10 || chr.Inventory.Mesos < mesos) return;
            chr.Inventory.RemoveMesos(mesos, false);
            Point targetPosition = chr.Map.GetDropPositionBelow(new Point(chr.Position.X, chr.Position.Y - 50), chr.Position);
            chr.Map.SpawnMesoMapItem(mesos, chr.Position, targetPosition, true, MapleDropType.Unk, chr);
            chr.EnableActions(false);
        }
    }
}
