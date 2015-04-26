using LeattyServer.ServerInfo.Player;

namespace LeattyServer.ServerInfo.Packets.Handlers
{
    public static class HyperskillInfoRequestHandler
    {
        public static void Handle(MapleClient c, PacketReader pr)
        {
            string hyper = pr.ReadMapleString();
            int id = pr.ReadInt();
            int tab = pr.ReadInt();

            c.SendPacket(HyperskillInfo(hyper, id, tab, 0));
        }

        public static PacketWriter HyperskillInfo(string hyper, int id, int tab, int maxLevel)
        {
            PacketWriter pw = new PacketWriter(SendHeader.HyperskillInfo);

            pw.WriteMapleString(hyper);
            pw.WriteInt(id);
            pw.WriteInt(tab);

            pw.WriteBool(maxLevel > 0); //enables/disables it
            pw.WriteInt(maxLevel);

            return pw;
        }
    }
}