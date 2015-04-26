using System.Collections.Generic;
using System.Linq;
using LeattyServer.ServerInfo.Player;

namespace LeattyServer.ServerInfo.Packets.Handlers
{
    public static class SpecialChatHandler
    {
        public static void Handle(MapleClient c, PacketReader pr)
        {
            byte type = pr.ReadByte();
            byte recipientCount = pr.ReadByte();
            if (recipientCount <= 0) return;
            int[] recipients = new int[recipientCount];
            for (int i = 0; i < recipients.Length; i++)
                recipients[i] = pr.ReadInt();
            string text = pr.ReadMapleString();
            MapleCharacter chr = c.Account.Character;
            List<MapleClient> validClients = new List<MapleClient>();
            switch (type)
            {
                case 0x00: //Buddy
                {
                    foreach (int id in recipients)
                    {
                        MapleClient recipientClient = Program.GetClientByCharacterId(id);
                        if (recipientClient != null && chr.BuddyList.MapleCharacterIsBuddy(recipientClient.Account.Character))
                                validClients.Add(recipientClient);
                    }
                    break;
                }
                case 0x01: //Party
                {
                    if (chr.Party == null) return;
                    foreach (int id in recipients)
                    {
                        if (!chr.Party.CharacterIdIsMember(id)) continue;
                        MapleClient recipientClient = Program.GetClientByCharacterId(id);
                        if (recipientClient != null)
                            validClients.Add(recipientClient);
                    }
                    break;
                }
                case 0x02: //Guild
                {
                    if (chr.Guild == null) return;
                    foreach (int id in recipients)
                    {
                        if (!chr.Guild.HasCharacter(id)) continue;
                        MapleClient recipientClient = Program.GetClientByCharacterId(id);
                        if (recipientClient != null)
                            validClients.Add(recipientClient);
                    }
                    break;
                }
                case 0x03: //Alliance
                {
                    break;
                }
                case 0x04: //Expedition
                {
                    break;
                }
            }
            if (!validClients.Any()) return;
            PacketWriter pw = new PacketWriter(SendHeader.SpecialChat);
            pw.WriteByte(type);
            pw.WriteMapleString(chr.Name);
            pw.WriteMapleString(text);

            foreach (MapleClient recipientClient in validClients)
                recipientClient.SendPacket(pw);
        } 
    }
}