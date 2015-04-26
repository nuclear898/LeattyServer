using LeattyServer.Constants;
using LeattyServer.ServerInfo.Player;

namespace LeattyServer.ServerInfo.Packets.Handlers
{
    public static class ChangeChannelHandler
    {
        public static void Handle(MapleClient c, PacketReader pr)
        {
            if (!c.Account.Character.DisableActions()) return;
            if (c.Account.Character.Map.ChangeChannelLimit)
                return;
            byte channel = pr.ReadByte();
            if (Program.ChannelServers.ContainsKey(channel))
            {
                ushort port = Program.ChannelServers[channel].Port;
                //Theres also 1 int left, timestamp I asume, against cc spam
                PacketWriter pw = new PacketWriter();
                pw.WriteHeader(SendHeader.ChangeChannelResponse);
                pw.WriteBool(true);
                pw.WriteBytes(ServerConstants.NexonIp[1]);
                pw.WriteUShort(port);
                if (c.Account.MigrationData.ToCashShop)
                {
                    c.Account.MigrationData.ToCashShop = false;
                }
                else
                {
                    pw.WriteByte(0);
                }
                c.Account.MigrationData.ToChannel = channel;
                c.Account.MigrationData.CheatTracker = c.CheatTracker;
                c.Account.MigrationData.Character = c.Account.Character;
                Program.EnqueueMigration(c.Account.MigrationData.CharacterId, c.Account.MigrationData);
                c.SendPacket(pw);
            }
            else
            {
                PacketWriter pw = new PacketWriter();
                pw.WriteHeader(SendHeader.ChangeChannelResponse);
                pw.WriteBool(false);
                c.SendPacket(pw);
                c.Account.Character.EnableActions(false);
            }
        }
    }
}
