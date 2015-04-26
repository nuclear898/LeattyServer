using LeattyServer.Helpers;
using LeattyServer.ServerInfo.Player;

namespace LeattyServer.ServerInfo.Packets.Handlers
{
    class ClientLoadedHandler
    {
        public static void Handle(MapleClient c)
        {
            PacketWriter pw = new PacketWriter();
            pw.WriteHeader(SendHeader.ClientLoaded);
            //pw.WriteByte(0); //Amount of strings, 0 = none       
            Red(pw);
            c.SendPacket(pw);
        }

        public static void Xenon(PacketWriter pw)
        {
            pw.WriteByte(4); //Amount of strings
            pw.WriteMapleString("dsub");
            pw.WriteByte(0);
            pw.WriteMapleString("dmain");
            pw.WriteByte(0); 
            pw.WriteMapleString("xsub");
            pw.WriteBool(Functions.RandomBoolean()); //channel screen image
            pw.WriteMapleString("xmain");
            pw.WriteByte(1);
        }

        public static void Avenger(PacketWriter pw)
        {
            pw.WriteByte(4); //Amount of strings
            pw.WriteMapleString("xsub");
            pw.WriteByte(0);
            pw.WriteMapleString("xmain");
            pw.WriteByte(0); 
            pw.WriteMapleString("dsub");
            pw.WriteBool(Functions.RandomBoolean()); //channel screen image
            pw.WriteMapleString("dmain");
            pw.WriteByte(1); 
        }

        //02 04 00 72 65 64 32 00 04 00 72 65 64 31 01
        public static void Red(PacketWriter pw)
        {
            //beast tamer:
            //[02] [04 00] [72 65 64 31] [00] [04 00] 7[2 65 64 32] [01]
            pw.WriteByte(2);
            pw.WriteMapleString("red1");
            pw.WriteByte(0);
            pw.WriteMapleString("red2");
            pw.WriteByte(1);
        }
    }
}
