using LeattyServer.ServerInfo.Player;

namespace LeattyServer.ServerInfo.Packets.Handlers
{
    public static class FacialExpressionHandler
    {
        public static void Handle(MapleClient c, PacketReader pr)
        {
            int emoId = pr.ReadInt();
            int unk = pr.ReadInt(); //??
            byte unk2 = pr.ReadByte();
            c.Account.Character.Map.BroadcastPacket(FacialExpressionPacket(c.Account.Character.Id, emoId, unk, unk2));
        }

        public static PacketWriter FacialExpressionPacket(int characterId, int emoId, int unk1, byte unk2)
        {
            PacketWriter pw = new PacketWriter(SendHeader.FacialExpression);
            pw.WriteInt(characterId);
            pw.WriteInt(emoId);
            pw.WriteInt(unk1);
            pw.WriteByte(unk2);

            return pw;
        }
    }
}
