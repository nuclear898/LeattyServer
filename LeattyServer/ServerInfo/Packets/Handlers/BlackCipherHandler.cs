using LeattyServer.ServerInfo.Player;

namespace LeattyServer.ServerInfo.Packets.Handlers
{
    class BlackCipherHandler
    {
        public static void Handle(int i, MapleClient c)
        {
            i += 0; //sometimes changes with maple version, can be +1 or +2 or +3, unknown why
            int x = ((i >> 5) << 5) + (((((i & 0x1F) >> 3) ^ 2) << 3) + (7 - (i & 7)));
            x |= (i >> 7) << 7;

            PacketWriter pw = new PacketWriter();
            pw.WriteHeader(SendHeader.BlackCipher);
            pw.WriteInt(x);
            c.SendPacket(pw);
        }
    }
}
