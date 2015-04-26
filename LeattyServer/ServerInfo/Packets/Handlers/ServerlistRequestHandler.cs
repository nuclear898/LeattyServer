using LeattyServer.Constants;
using LeattyServer.ServerInfo.Player;

namespace LeattyServer.ServerInfo.Packets.Handlers
{
    class ServerlistRequestHandler
    {
        public static void Handle(MapleClient c)
        {
            //Todo, Loop for each world and channel
            PacketWriter pw = new PacketWriter();
            pw.WriteHeader(SendHeader.ShowWorlds);
            pw.WriteByte(0); //WorldId
            pw.WriteMapleString("Scania"); //World name
            pw.WriteByte(2); //Flag
            pw.WriteMapleString("Leatty!"); //Event message
            pw.WriteShort(0x64);
            pw.WriteShort(0x64);
            pw.WriteByte(0);

            byte channelCount = ServerConstants.Channels;
            pw.WriteByte(channelCount); //Channelcount

            for (short i = 0; i < channelCount; i++)
            {
                pw.WriteMapleString("Scania-" + i.ToString()); //channel name
                pw.WriteInt(0); //load
                pw.WriteByte(0); //World id again? o-o'
                pw.WriteShort(i); //channelid - 1
            }

            pw.WriteZeroBytes(8); //8 empty bytes
            c.SendPacket(pw);
            
            pw = new PacketWriter();
            pw.WriteHeader(SendHeader.ShowWorlds);
            pw.WriteByte(0xFF);
            pw.WriteByte(0);
            c.SendPacket(pw);
        }
    }
}
