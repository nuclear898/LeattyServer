using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Web.WebPages.Instrumentation;
using LeattyServer.Helpers;
using LeattyServer.ServerInfo.Packets;
using LeattyServer.ServerInfo.Player;

namespace LeattyServer.ServerInfo
{
    public class MapleMessengerRoom
    {
        private static readonly AutoIncrement IdCounter = new AutoIncrement(1);

        public static readonly Dictionary<int, MapleMessengerRoom> Rooms = new Dictionary<int, MapleMessengerRoom>();

        public int Id { get; set; }
        public int Capacity { get; set; }


        private readonly Dictionary<int, MapleMessengerCharacter> Participants = new Dictionary<int, MapleMessengerCharacter>();

        public MapleMessengerRoom(int capacity)
        {
            Id = IdCounter.Get;
            Capacity = capacity;
            Rooms.Add(Id, this);
        }

        public static MapleMessengerRoom GetChatRoom(int id)
        {
            MapleMessengerRoom ret;
            return Rooms.TryGetValue(id, out ret) ? ret : null;
        }

        public bool AddPlayer(MapleCharacter chr)
        {
            if (Participants.ContainsKey(chr.Id)) return false;
            int position = GetFreePosition();
            if (position == -1) return false; //No space
            MapleMessengerCharacter mcc = new MapleMessengerCharacter(position, chr);
            chr.ChatRoom = this;
            chr.Client.SendPacket(Packets.EnterRoom((byte)position));
            var playerAddPacket = Packets.AddPlayer(mcc);
            BroadCastPacket(playerAddPacket, chr.Id);
            foreach (MapleMessengerCharacter participant in Participants.Values)
            {
                chr.Client.SendPacket(Packets.AddPlayer(participant));
            }
            Participants.Add(chr.Id, mcc);
            return true;
        }

        public void RemovePlayer(int chrId)
        {
            MapleMessengerCharacter mcc;
            if (!Participants.TryGetValue(chrId, out mcc)) return;
            Participants.Remove(chrId);
            mcc.Character = null;
            if (!Participants.Any()) //All players are gone
            {
                Rooms.Remove(Id);
                return;
            }
            var removePacket = Packets.PlayerLeft((byte) mcc.Position);
            BroadCastPacket(removePacket);
        }
        
        public MapleCharacter GetChatCharacterByName(string name) => Participants.Select(x => x.Value.Character).FirstOrDefault(x => x.Name == name);

     
        public void DoChat(int characterIdFrom, string message)
        {
            MapleMessengerCharacter mcc;
            if (!Participants.TryGetValue(characterIdFrom, out mcc)) return;
            var chatPacket = Packets.Chat(mcc.Character.Name, message);
            BroadCastPacket(chatPacket, characterIdFrom);
        }

        private int GetFreePosition()
        {
            bool[] currentPositions = new bool[Capacity];
            foreach (MapleMessengerCharacter mcc in Participants.Values)
            {
                currentPositions[mcc.Position] = true;
            }
            for (int i = 0; i < Capacity; i++)
            {
                if (!currentPositions[i])
                    return i;
            }
            return -1;
        }

        private void BroadCastPacket(PacketWriter packet, int characterSourceId = 0, bool repeatToSource = false)
        {
            foreach (MapleCharacter chr in Participants.Select(x => x.Value.Character))
            {
                if (repeatToSource || chr.Id != characterSourceId)
                {
                    chr.Client.SendPacket(packet);
                }
            }
        }

        public class MapleMessengerCharacter
        {
            public int Position { get; }
            public MapleCharacter Character { get; set; }

            public MapleMessengerCharacter(int position, MapleCharacter chr)
            {
                Position = position;
                Character = chr;
            }
        }

        public static class Packets
        {
            public static PacketWriter EnterRoom(byte position)
            {
                PacketWriter pw = new PacketWriter(SendHeader.Messenger);
                pw.WriteByte(1);
                pw.WriteByte(position);
                return pw;
            }

            public static PacketWriter PlayerLeft(byte position)
            {
                PacketWriter pw = new PacketWriter(SendHeader.Messenger);
                pw.WriteByte(2);
                pw.WriteByte(position);
                return pw;
            }

            public static PacketWriter Chat(string characterName, string message)
            {
                //06 07 00 4E 75 6B 65 4B 75 6E 04 00 20 3A 20 31
                PacketWriter pw = new PacketWriter(SendHeader.Messenger);
                pw.WriteByte(6);
                pw.WriteMapleString(characterName);
                pw.WriteMapleString(message);
                return pw;
            }

            public static PacketWriter AddPlayer(MapleMessengerCharacter mcc)
            {
                MapleCharacter player = mcc.Character;
                PacketWriter pw = new PacketWriter(SendHeader.Messenger);
                pw.WriteByte(0);
                pw.WriteByte((byte)mcc.Position);
                MapleCharacter.AddCharLook(pw, player, false);

                pw.WriteMapleString(player.Name);
                pw.WriteByte(player.Client.Channel);
                pw.WriteByte(1);
                pw.WriteShort(player.Job);
                pw.WriteShort(player.SubJob);

                return pw;
            }
        }
    }
}
