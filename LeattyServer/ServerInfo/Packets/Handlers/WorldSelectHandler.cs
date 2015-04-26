using LeattyServer.ServerInfo.Player;

namespace LeattyServer.ServerInfo.Packets.Handlers
{
    class WorldSelectHandler
    {
        public static void Handle(short WorldId, MapleClient c)
        {
            PacketWriter pw = new PacketWriter();
            pw.WriteHeader(SendHeader.ServerStatus);
            // 0 = Select world normally                                        00
            // 1 = "Since there are many users, you may encounter some..."      01      
            // 2 = "The concurrent users in this world have reached the max"    10
            pw.WriteShort(0);
            c.SendPacket(pw);
        }
    }
}
