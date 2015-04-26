using LeattyServer.Constants;
using LeattyServer.Helpers;
using LeattyServer.ServerInfo.Player;

namespace LeattyServer.ServerInfo.Packets.Handlers
{
    class ReceiveHandShakeHandler
    {
        public static void Handle(MapleClient c, PacketReader pr)
        {
            ushort gameVersion = pr.ReadUShort();
            pr.Skip(1);
            ushort subVersion = pr.ReadUShort();

            if (gameVersion == ServerConstants.Version || subVersion == ServerConstants.SubVersion)
            {
                c.SendPacket(EnableLoginButton());
            }
            else
            {
                ServerConsole.Warning("Client connected with version: " + gameVersion + "." + subVersion + " while server is " + ServerConstants.Version + "." + ServerConstants.SubVersion);
            }
        }

        /// <summary>
        /// Packet that enables the login screen Login Button
        /// </summary>
        /// <returns></returns>
        static PacketWriter EnableLoginButton()
        {
            PacketWriter pw = new PacketWriter(SendHeader.HackShieldHeartBeat);
            pw.WriteByte(0x7);
            return pw;
        }
    }
}
