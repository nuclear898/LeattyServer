using System;
using LeattyServer.Constants;
using LeattyServer.Helpers;
using LeattyServer.ServerInfo.Player;

namespace LeattyServer.ServerInfo.Packets.Handlers
{
    class ChooseCharWithPicHandler
    {
        public static void Handle(MapleClient c, PacketReader pr)
        {
            try
            {
                if (c.Account == null)
                {
                    c.Disconnect("Client is not logged in to an account");
                    return;
                }

                string pic = pr.ReadMapleString();
                int characterId = pr.ReadInt();
                pr.Skip(1);
                string macs = pr.ReadMapleString();
                string clientid = pr.ReadMapleString();
                if (c.Account.CheckPic(pic) && c.Account.HasCharacter(characterId) && Program.ChannelServers.ContainsKey(c.Channel))
                {
                    ushort port = Program.ChannelServers[c.Channel].Port;
                    c.Account.MigrationData.CharacterId = characterId;
                    c.Account.MigrationData.Character = MapleCharacter.LoadFromDatabase(characterId, false);
                    c.Account.MigrationData.Character.Hidden = c.Account.IsGM;
                    
                    Program.EnqueueMigration(characterId, c.Account.MigrationData);
                    
                    c.SendPacket(ChannelIpPacket(port, characterId));
                }
                else
                {
                    //Incorrect Pic
                    PacketWriter pw = new PacketWriter();
                    pw.WriteHeader(SendHeader.PICResponse);
                    pw.WriteByte(0x14);
                    c.SendPacket(pw);
                }
            }
            catch (Exception ex)
            {
                ServerConsole.Error(ex.ToString());
                FileLogging.Log("Character loading", ex.ToString());
                c.SendPacket(MapleCharacter.ServerNotice("Error loading character", 1));
                /*PacketWriter pw = new PacketWriter();
                pw.WriteHeader(SendHeaders.PICResponse);
                pw.WriteByte(0x15);
                c.SendPacket(pw);*/
            }
        }

        public static PacketWriter ChannelIpPacket(ushort port, int characterId)
        {        
            //[00] [00] [[08] [1F] [63] [B4]] [8B 21] 00 00 00 00 00 00 [6B 9F 6D 00] 00 00 00 00 00 00 00 78 CC 2B 00 00 62 64 62

            PacketWriter pw = new PacketWriter(SendHeader.ChannelIp);          
           
            pw.WriteByte(0);
            pw.WriteByte(0);
            pw.WriteBytes(ServerConstants.NexonIp[1]);
            pw.WriteUShort(port);
            pw.WriteZeroBytes(6); //new v148
            pw.WriteInt(characterId);
            pw.WriteByte(0);
            pw.WriteInt(0);
            pw.WriteByte(0);
            pw.WriteHexString("BC 00 00 00 00 00 00 00 00");

            return pw;
        }
    }
}
