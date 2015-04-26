using System;
using LeattyServer.ServerInfo.Player;

namespace LeattyServer.ServerInfo.Packets.Handlers
{
    class PongHandler
    {
        public static void Handle(MapleClient c)
        {
            c.LastPong = DateTime.UtcNow;
        }

        public static PacketWriter PingPacket()
        {
            PacketWriter pw = new PacketWriter();
            pw.WriteHeader(SendHeader.Ping);            
            return pw;
        }
    }
}
