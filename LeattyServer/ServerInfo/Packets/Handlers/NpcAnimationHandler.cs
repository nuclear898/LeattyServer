using LeattyServer.ServerInfo.Player;

namespace LeattyServer.ServerInfo.Packets.Handlers
{
    class NpcAnimationHandler
    {
        public static void Handle(MapleClient c, PacketReader pr)
        {
            PacketWriter pw = new PacketWriter();
            pw.WriteHeader(SendHeader.NpcAnimation);

            int availableBytes = (int)pr.Available;
            if (availableBytes == 10) //npc talk;
            {
                pw.WriteInt(pr.ReadInt());
                pw.WriteShort(pr.ReadShort());
                pw.WriteInt(pr.ReadInt());
            }
            else if (availableBytes > 10) //npc move
            {
                availableBytes -= 9;
                byte[] bytes = pr.ReadBytes(availableBytes);
                pw.WriteBytes(bytes);
            }            
            
            c.SendPacket(pw);
        }
    }
}
