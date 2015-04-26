using LeattyServer.Helpers;
using LeattyServer.ServerInfo.Commands;
using LeattyServer.ServerInfo.Player;

namespace LeattyServer.ServerInfo.Packets.Handlers
{
    class PlayerChatHandler
    {
        public static void Handle(MapleClient c, PacketReader pr)
        {
            int tickCount = pr.ReadInt();
            string message = pr.ReadMapleString();
            byte show = pr.ReadByte();

            ServerConsole.Info(c.Account.Character.Name + ": " + message);

            if (message[0] == '@')
            {
                if (PlayerCommands.ProcessCommand(message.Substring(1).Split(' '), c))
                    return;
            }
            else if (message[0] == '!')
            {
                if (c.Account.IsGM)
                {
                    string[] split = message.Substring(1).Split(' ');
                    if (GMCommands.ProcessCommand(split, c))
                        return;
                    if (c.Account.IsAdmin)
                    {
                        if (AdminCommands.ProcessCommand(split, c))
                            return;
                        else
                        {
                            c.Account.Character.SendBlueMessage("Unrecognized Admin command");
                            return;
                        }
                    }
                    else
                    {
                        c.Account.Character.SendBlueMessage("Unrecognized GM command");
                        return;
                    }
                }
            }
            else if (message[0] == '#')
            {
                if (c.Account.IsGM || c.Account.IsDonor)
                {
                    string[] split = message.Substring(1).Split(' ');
                    if (DonorCommands.ProcessCommand(split, c))
                        return;
                    else
                    {
                        c.Account.Character.SendBlueMessage("Unrecognized Donor command");
                        return;
                    }
                }
            }

            PacketWriter packet = PlayerChatPacket(c.Account.Character.Id, message, show, c.Account.IsGM);
            c.Account.Character.Map.BroadcastPacket(packet);
        }

        public static PacketWriter PlayerChatPacket(int characterId, string message, byte show, bool whiteBackground)
        {
            PacketWriter pw = new PacketWriter();
            pw.WriteHeader(SendHeader.PlayerChat);

            pw.WriteInt(characterId);
            pw.WriteBool(whiteBackground);
            pw.WriteMapleString(message);
            pw.WriteByte(show);
            pw.WriteBool(false);//isWorldMessage
            pw.WriteByte(0xFF);//if isWorldMessage, this is worldID

            return pw;
        }
    }
}
