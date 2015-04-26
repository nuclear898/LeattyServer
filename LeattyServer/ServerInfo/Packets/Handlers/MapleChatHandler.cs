using LeattyServer.ServerInfo.Player;

namespace LeattyServer.ServerInfo.Packets.Handlers
{
    public static class MapleMessengerHandler
    {
        public static void Handle(MapleClient c, PacketReader pr)
        {
            MapleCharacter chr = c.Account.Character;
            byte action = pr.ReadByte();
            switch (action)
            {
                case 0x1: //Open
                    {
                        //random room for two
                        //00 01 02 00 00 00 00

                        //random group
                        //00 01 06 00 00 00 00

                        //buddy
                        //00 00 06 00 00 00 00
                        bool random = pr.ReadBool();
                        int participants = pr.ReadByte();
                        if (participants < 2 || participants > 6) return;
                        int roomId = pr.ReadInt();
                        if (roomId == 0)
                        {
                            if (chr.ChatRoom != null) return;
                            MapleMessengerRoom room = new MapleMessengerRoom(participants);
                            room.AddPlayer(chr);
                        }
                        else
                        {
                            if (chr.ChatRoom != null) return;
                            MapleMessengerRoom.GetChatRoom(roomId)?.AddPlayer(chr);
                        }
                        break;
                    }
                case 0x2: //Leave room
                    {
                        chr.ChatRoom?.RemovePlayer(chr.Id);
                        chr.ChatRoom = null;
                        break;
                    }
                case 0x3: //Invite
                    {
                        //03 06 00 4B 61 7A 72 6F 6C
                        if (chr.ChatRoom == null) return;
                        string name = pr.ReadMapleString();
                        MapleClient invitedClient = Program.GetClientByCharacterName(name);
                        if (invitedClient != null && !invitedClient.Account.Character.Hidden)
                        {
                            invitedClient.SendPacket(InvitePacket(chr.Name, c.Channel, chr.ChatRoom.Id));
                            c.SendPacket(InviteSuccessPacket(name, true));
                        }
                        else
                        {
                            c.SendPacket(InviteSuccessPacket(name, false));
                        }
                        break;
                    }
                case 0x5: //decline invitation
                    {
                        string inviterName = pr.ReadMapleString();
                        string myName = pr.ReadMapleString();
                        bool accept = pr.ReadBool();
                        Program.GetClientByCharacterName(inviterName)?.SendPacket(InviteResponsePacket(chr.Name, false));
                        break;
                    }
                case 0x6: //Chat
                    {
                        if (chr.ChatRoom == null) return;
                        string name = pr.ReadMapleString();
                        string text = pr.ReadMapleString();
                        chr.ChatRoom.DoChat(chr.Id, text);
                        break;
                    }
                case 0xB: //View info
                {
                        //0B 06 00 4B 61 7A 72 6F 6C
                        string name = pr.ReadMapleString();
                        MapleCharacter target = chr.ChatRoom?.GetChatCharacterByName(name);
                        if (target != null)
                        {
                            c.SendPacket(ParticipantInfoPacket(target));
                        }
                        break;
                    }
                case 0xF: //Eileen here!... NPC saying random stuff
                    {
                        PacketWriter pw = new PacketWriter(SendHeader.Messenger);
                        pw.WriteByte(action);
                        pw.WriteByte(pr.ReadByte());
                        c.SendPacket(pw);
                        break;
                    }
            }
        }

        private static PacketWriter InvitePacket(string characterNameFrom, byte channelFrom, int chatRoomId)
        {
            //[03] [07 00 4E 75 6B 65 4B 75 6E] [03] [D5 0B 00 00] [00]
            PacketWriter pw = new PacketWriter(SendHeader.Messenger);
            pw.WriteByte(3);
            pw.WriteMapleString(characterNameFrom);
            pw.WriteByte(channelFrom);
            pw.WriteInt(chatRoomId);
            pw.WriteByte(0);
            return pw;
        }

        private static PacketWriter ParticipantInfoPacket(MapleCharacter participant)
        {
            //[0B] [06 00 4B 61 7A 72 6F 6C] [15] [90 01] [00 00] [00 00 00 00] [00 00 00 00] [01 00 2D] [00 00] [02]
            PacketWriter pw = new PacketWriter(SendHeader.Messenger);
            pw.WriteByte(0xB);
            pw.WriteMapleString(participant.Name);
            pw.WriteByte(participant.Level);
            pw.WriteShort(participant.Job);
            pw.WriteShort(participant.SubJob);
            pw.WriteInt(participant.Fame);
            pw.WriteInt(0); //Likeability
            pw.WriteMapleString(participant.Guild != null ? participant.Guild.Name : "-");
            pw.WriteShort(0); //Alliance name
            pw.WriteByte(2); //dunno
            return pw;
        }
       
        private static PacketWriter InviteSuccessPacket(string characterName, bool success)
        {
            PacketWriter pw = new PacketWriter(SendHeader.Messenger);
            pw.WriteByte(4);
            pw.WriteMapleString(characterName);
            pw.WriteBool(success);
            return pw;
        }

        private static PacketWriter InviteResponsePacket(string characterName, bool success)
        {
            //05 06 00 4B 61 7A 72 6F 6C 00
            PacketWriter pw = new PacketWriter(SendHeader.Messenger);
            pw.WriteByte(5);
            pw.WriteMapleString(characterName);
            pw.WriteBool(success);
            return pw;
        }
    }
}