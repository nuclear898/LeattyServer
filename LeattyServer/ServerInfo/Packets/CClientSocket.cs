using System;
using System.Net;
using System.Net.Sockets;
using LeattyServer.Constants;
using LeattyServer.Crypto;
using LeattyServer.ServerInfo.Player;

namespace LeattyServer.ServerInfo.Packets
{
    public class CClientSocket : IDisposable
    {
        private readonly Socket socket;
        private readonly byte[] socketbuffer;
        private readonly string host;
        private readonly int port;
        private readonly object dispose_Sync;
        private readonly MapleClient client;
        private bool disposed;

        public CipherHelper Crypto { get; private set; }
        public bool Connected
        {
            get
            {
                return disposed == false;
            }
        }
        public string Host
        {
            get
            {
                return host;
            }
        }
        public int Port
        {
            get
            {
                return port;
            }
        }
        public CClientSocket(MapleClient pClient, Socket sock)
        {
            socket = sock;
            socketbuffer = new byte[1024];

            host = ((IPEndPoint)socket.RemoteEndPoint).Address.ToString();
            port = ((IPEndPoint)socket.LocalEndPoint).Port;

            dispose_Sync = new object();

            client = pClient;

            Crypto = new CipherHelper(ServerConstants.Version);

            Crypto.PacketFinished += (data) =>
            {
                pClient.RecvPacket(new PacketReader(data));
            };

            WaitForData();
        }
        private void WaitForData()
        {
            if (!disposed)
            {
                SocketError error = SocketError.Success;

                socket.BeginReceive(socketbuffer, 0, socketbuffer.Length, SocketFlags.None, out error, OnPacketReceived, null);

                if (error != SocketError.Success)
                {
                    Disconnect();
                }
            }
        }
        public void SendRawPacket(byte[] final)
        {
            if (!disposed)
            {
                int offset = 0;

                while (offset < final.Length)
                {
                    SocketError outError = SocketError.Success;
                    int sent = socket.Send(final, offset, final.Length - offset, SocketFlags.None, out outError);

                    if (sent == 0 || outError != SocketError.Success)
                    {
                        Disconnect();
                        return;
                    }

                    offset += sent;
                }
            }
        }
        public void SendPacket(PacketWriter data)
        {
            if (!disposed)
            {
                var buffer = data.ToArray();
                Crypto.Encrypt(ref buffer, true);
                SendRawPacket(buffer);
            }
        }
        private void OnPacketReceived(IAsyncResult iar)
        {
            if(!disposed)
            {
                SocketError error = SocketError.Success;
                int size = socket.EndReceive(iar, out error);

                if (size == 0 || error != SocketError.Success)
                {
                    Disconnect();
                }
                else
                {
                    Crypto.AddData(socketbuffer, 0, size);
                    WaitForData();
                }
            }
        }
        public void Disconnect()
        {
            Dispose();
        }
        public void Dispose()
        {
            lock (dispose_Sync)
            {
                if (disposed == true)
                    return;

                disposed = true;

                try
                {
                    socket.Shutdown(SocketShutdown.Both);
                    socket.Close();
                }
                finally
                {
                    client.Disconnected();
                }
            }
        }
        ~CClientSocket()
        {
            Dispose();
        }
    }
}
