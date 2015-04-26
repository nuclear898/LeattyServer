using LeattyServer.ServerInfo.Player;

namespace LeattyServer.ServerInfo.Packets.Handlers
{
    class FindPlayerHandler
    {
        public static void Handle(MapleClient c, PacketReader pr)
        {
            byte type = pr.ReadByte();
            int tickCount = pr.ReadInt();
            switch (type)
            {
                case 5: // Find command
                case 0x44:
                    {
                        string name = pr.ReadMapleString();
                        MapleCharacter target = Program.GetCharacterByName(name);
                        bool buddyList = type == 0x44; //Updates info in the buddylist
                        if (target != null && (!target.Hidden || c.Account.IsGM))
                        {
                            if (c.Channel == target.Client.Channel)
                                c.SendPacket(FindResponseSameChannel(name, target.MapId, buddyList));
                            else
                                c.SendPacket(FindResponseOtherChannel(name, target.Client.Channel, buddyList));
                        }
                        else if (!buddyList)
                        {
                            c.SendPacket(FindResponseFailure(name));
                        }
                        break;
                    }
                case 6: // Whisper
                    {
                        string receiverName = pr.ReadMapleString();
                        string message = pr.ReadMapleString();
                        MapleCharacter receiver = Program.GetCharacterByName(receiverName);
                        bool success = false;
                        if (receiver != null && (!receiver.Hidden || c.Account.IsGM))
                        {
                            receiver.Client.SendPacket(ReceiveWhisper(c.Account.Character.Name, message, (short)(c.Channel)));
                            success = true;
                        }
                        c.SendPacket(WhisperResponse(receiverName, success));
                        break;
                    }
            }
        }

        //same channel:
        //09 0C 00 54 68 65 48 6F 6C 79 53 74 69 6E 67 01 00 E1 F5 05 2A 09 00 00 4E 01 00 00        
        public static PacketWriter FindResponseSameChannel(string name, int mapId, bool buddy)
        {
            PacketWriter pw = new PacketWriter();
            pw.WriteHeader(SendHeader.FindPlayerResponse);
            pw.WriteByte(buddy ? (byte)0x72 : (byte)0x9);
            pw.WriteMapleString(name);
            pw.WriteByte(1);
            pw.WriteInt(mapId);
            pw.WriteInt(0);
            pw.WriteInt(0);
            return pw;
        }

        //other channel:
        //09 0C 00 74 68 65 68 6F 6C 79 73 74 69 6E 67 [03] [00 00 00 00]
        public static PacketWriter FindResponseOtherChannel(string name, int channel, bool buddy)
        {
            PacketWriter pw = new PacketWriter();
            pw.WriteHeader(SendHeader.FindPlayerResponse);
            pw.WriteByte(buddy ? (byte)0x72 : (byte)0x9);
            pw.WriteMapleString(name);
            pw.WriteByte(3);
            pw.WriteInt(channel);
            return pw;
        }

        //Unable to find:
        //09 0C 00 74 68 65 68 6F 6C 79 73 74 69 6E 61 [00] [FF FF FF FF]
        public static PacketWriter FindResponseFailure(string name)
        {
            PacketWriter pw = new PacketWriter();
            pw.WriteHeader(SendHeader.FindPlayerResponse);
            pw.WriteByte(9);
            pw.WriteMapleString(name);
            pw.WriteByte(0);
            pw.WriteInt(-1);
            return pw;
        }

        public static PacketWriter WhisperResponse(string receiver, bool success)
        {
            PacketWriter pw = new PacketWriter();
            pw.WriteHeader(SendHeader.FindPlayerResponse);
            pw.WriteByte(10);
            pw.WriteMapleString(receiver);
            pw.WriteBool(success);
            return pw;
        }

        public static PacketWriter ReceiveWhisper(string sender, string message, short channelFrom)
        {
            PacketWriter pw = new PacketWriter();
            pw.WriteHeader(SendHeader.FindPlayerResponse);
            pw.WriteByte(18);
            pw.WriteMapleString(sender);
            pw.WriteShort(channelFrom);
            pw.WriteMapleString(message);

            return pw;
        }
    }
}
