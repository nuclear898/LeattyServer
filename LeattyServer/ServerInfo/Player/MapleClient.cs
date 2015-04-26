using System;
using System.Data.Entity;
using System.Linq;
using LeattyServer.Constants;
using LeattyServer.Crypto;
using LeattyServer.Data;
using LeattyServer.Helpers;
using LeattyServer.Data.WZ;
using LeattyServer.ServerInfo.AntiCheat;
using LeattyServer;
using System.Net.Sockets;
using LeattyServer.Data.Scripts;
using LeattyServer.ServerInfo.Packets;

namespace LeattyServer.ServerInfo.Player
{
    public class MapleClient
    {
        public String Host { get; set; }
        public int Port { get; set; }
        public CClientSocket SClient { get; set; }
        public MapleAccount Account { get; set; }
        public NpcEngine NpcEngine { get; set; }
        public OffenceTracker CheatTracker { get; set; }
        public byte Channel { get; set; }
        public bool Connected { get; set; }
        public LimitedQueue<PacketWriter> LastPacketsSent { get; set; }
        public DateTime LastPong { get; set; }

        public MapleClient(Socket session)
        {
            SClient = new CClientSocket(this, session);
            Host = SClient.Host;
            Port = SClient.Port;
            ServerConsole.Info(String.Format("{0}:{1} Connnected", Host, Port));
            Channel = 1;
            Connected = true;
            LastPacketsSent = new LimitedQueue<PacketWriter>(10);
            CheatTracker = new OffenceTracker() { Client = this };
        }

        internal void RecvPacket(PacketReader packet)
        {
            try
            {
                RecvPacketHandler.Handle(packet, this);
            }
            catch (Exception e)
            {
                ServerConsole.Error(e.ToString());
                FileLogging.Log("PacketExceptions.txt", e.ToString());
            }
        }

        public void Disconnect(string reason, params object[] values)
        {
            Console.WriteLine("Disconnected client with reason: " + string.Format(reason, values));
            if (SClient != null)
                SClient.Disconnect();
        }

        internal void Disconnected()
        {
            MapleCharacter save = null;
            if (Account != null)
                save = Account.Character;

            try
            {
                if (Account != null)
                {
                    Account.Release(); //It is imperative to release the account before removing the client from the server.
                }
            }
            catch { }
            try
            {
                if (Connected)
                {
                    ServerConsole.Info(String.Format("{0}:{1} Disconnected", Host, Port));
                }
                Connected = false;
                Program.RemoveClient(this);
                if (save != null)
                    save.LoggedOut();
            }
            catch { }
            try
            {

                if (NpcEngine != null)
                    NpcEngine.Dispose();
            }
            catch { }
            try
            {
                SClient.Dispose();
            }
            catch { }
        }

        public void SendPacket(PacketWriter packet)
        {
            if (ServerConstants.PrintPackets)
            {
                ServerConsole.Info(String.Format("Sending: {0}", Functions.ByteArrayToStr(packet.ToArray())));
            }

            if (SClient == null) return;

            SClient.SendPacket(packet);
            //LastPacketsSent.Enqueue(packet);
        }
        
        public void SendHandshake()
        {
            if (SClient == null) return;

            uint sIV = Functions.RandomUInt();
            uint rIV = Functions.RandomUInt();

            SClient.Crypto.SetVectors(sIV, rIV);

            PacketWriter writer = new PacketWriter();
            writer.WriteShort(0x0E);
            writer.WriteUShort(ServerConstants.Version);
            writer.WriteMapleString(ServerConstants.SubVersion.ToString());
            writer.WriteUInt(rIV);
            writer.WriteUInt(sIV);
            writer.WriteByte(8); //ServerIdent
            SClient.SendRawPacket(writer.ToArray());
        }
    }
}
