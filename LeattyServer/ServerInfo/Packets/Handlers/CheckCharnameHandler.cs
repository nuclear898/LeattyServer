using System;
using LeattyServer.ServerInfo.Player;

namespace LeattyServer.ServerInfo.Packets.Handlers
{
    class CheckCharnameHandler
    {
        public static void Handle(MapleClient c, String name)
        {
            PacketWriter pw = new PacketWriter();
            pw.WriteHeader(SendHeader.CharacterNameResponse);
            pw.WriteMapleString(name);
            pw.WriteBool(MapleCharacter.CharacterExists(name));
            c.SendPacket(pw);
        }
    }
}
