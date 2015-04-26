using System;
using System.Collections;
using System.Collections.Generic;
using LeattyServer.Constants;
using LeattyServer.Helpers;
using LeattyServer.ServerInfo.Player;

namespace LeattyServer.ServerInfo.Packets.Handlers
{
    public class ChooseChannelHandler
    {
        //opcode  ?    serv chan  network ip
        //[43 00] [02] [00] [00] [0A 00 00 03]
        public static void Handle(MapleClient c, PacketReader pr)
        {
            if (c.Account == null)
            {
                c.Disconnect("Account is not logged in"); //something's wrong, isnt logged in
                return;
            }

            pr.Skip(1);
            pr.Skip(1); //server, we only have 1 atm anyway
            byte channel = (byte)(pr.ReadByte());
            c.Account.MigrationData.ToChannel = channel;
            c.Channel = channel;
            //last 4 bytes = network IP

            /*PacketWriter pw = new PacketWriter();
            pw.WriteHeader(SendHeader.LoginSuccess);
            pw.WriteByte(0);
            pw.WriteInt(c.Account.Id); //Accid
            pw.WriteZeroBytes(10);
            pw.WriteByte(0x95);
            pw.WriteMapleString(c.Account.Name);
            pw.WriteZeroBytes(10);            
            pw.WriteMapleString(c.Account.Name);
            pw.WriteHexString("00 6E E5 D6 A3 2C C7 01"); //account creation date? 
            pw.WriteInt(10); //no idea
            pw.WriteHexString("E4 A4 AE E1 C5 54 E9 93"); //no idea
            pw.WriteShort(0);
            
            pw.WriteHexString(options);

            pw.WriteInt(-1);
            pw.WriteByte(0);
            pw.WriteByte(1);
            pw.WriteByte(1);
            c.SendPacket(pw);

            pw = new PacketWriter(SendHeader.WorldId);
            pw.WriteInt(0); //world 1 = scania
            c.SendPacket(pw);
            */

            c.SendPacket(ShowCharacters(c.Account));
            c.SendPacket(CreateCharacterOptions());
        }
        //static string options = "01 11 01 01 00 01 01 00 01 01 00 01 01 00 01 01 00 01 01 00 01 01 00 01 01 00 01 01 00 01 01 00 01 01 00 01 01 00 01 01 00 01 01 00 01 01 00 01 01 00 01 01 00 01 01 00 01 01 00 01 01 00 01 01 00 01 01 00 FF FF FF FF 00 01 01";
        //static string options = "01 11 01 01 00 01 01 00 01 01 00 01 01 00 01 01 00 01 01 00 01 01 00 01 01 00 01 01 00 01 01 00 01 01 00 01 01 00 01 01 00 01 01 00 01 01 00 01 01 00 01 01 00 01 01 00 01 01 00 01 01 00 01 01 00 01 01 00";
        //static string options = "01 11 01 01 00 01 01 00 01 01 00 01 01 00 01 01 00 01 01 00 01 01 00 01 01 00 01 01 00 01 01 00 01 01 00 01 01 00 01 01 00 01 01 00 01 01 00 01 01 00 01 01 00 01 01 00 01 01 00 01 01 00 00 01 00";
        //static string options = "01 11 01 01 00 01 01 00 01 01 00 01 01 00 01 01 00 01 01 00 01 01 00 01 01 00 01 01 00 01 01 00 01 01 00 01 01 00 01 01 00 01 01 00 01 01 00 00 02 00 01 01 00 01 01 00 01 01 00 01 01 00 00 01 00";
        static PacketWriter CreateCharacterOptions()
        {
            PacketWriter pw = new PacketWriter(SendHeader.CreateCharacterOptions);
            //pw.WriteHexString(options);
            pw.WriteBool(true);
            pw.WriteByte(16);

            foreach (var kvp in GameConstants.CreateJobOptions)
            {
                pw.WriteBool(kvp.Value); //enabled or not
                pw.WriteBool(false); // don't know
                pw.WriteBool(false); //?
            }
            //pw.WriteHexString("00 01 00");
            //pw.WriteHexString("00 01 00");
            /*for (int i = 0; i < 20; i++)
            {
                pw.WriteHexString("00 00 00");
            }*/
            //pw.WriteHexString("FF FF FF FF 00 01 01");

            return pw;
        }

        static PacketWriter ShowCharacters(MapleAccount acc)
        {
            List<MapleCharacter> chars = acc.GetCharsFromDatabase();
            PacketWriter pw = new PacketWriter(SendHeader.ShowCharacters);
            
            pw.WriteZeroBytes(5);
            pw.WriteLong(MapleFormatHelper.GetMapleTimeStamp(DateTime.UtcNow));
            pw.WriteByte((byte)chars.Count); //Char count            
            foreach (MapleCharacter chr in chars)
            {
                MapleCharacter.AddCharEntry(pw, chr);
            }
            //pw.WriteHexString("00 00 04 00 00 00 00 00 00 00 30 2A F3 A2 FA 33 D0 01 60 42 F9 64 00 00 00 00 00");
            
            pw.WriteByte(acc.HasPic() ? (byte)1 : (byte)0); //pic state? 4 = refresh, 1 = normal, 0 = create new pic
            pw.WriteByte(0);
            pw.WriteInt(4); //char slots
            pw.WriteInt(0);
            pw.WriteLong(MapleFormatHelper.GetMapleTimeStamp(DateTime.UtcNow));
            pw.WriteHexString("60 42 F9 64");
            pw.WriteInt(0); // 1 = namechange button
            return pw;
        }
    }
}
