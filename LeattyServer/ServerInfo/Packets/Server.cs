using System;
using System.Net;
using System.Net.Sockets;
using LeattyServer.Helpers;

namespace LeattyServer.ServerInfo.Packets
{

    public class Server : IDisposable
    {
        public delegate void ClientConnectedHandler(Socket client);

        public event ClientConnectedHandler OnClientConnected;

        public ushort Port = 0;

        TcpListener Listener;
        private const int BACKLOG_SIZE = 50; //The max number of clients waiting in the connection queue
        private bool Disposed = false;
        public Server()
        {
        }

        ~Server()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                ShutDown();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void ShutDown()
        {
            try
            {
                Disposed = true;
                Listener.Stop();
                Listener.Server.Shutdown(SocketShutdown.Both);
                Listener.Server.Close();
            }
            catch { }
        }

        public void Start(IPAddress ip, ushort port)
        {
            Listener = new TcpListener(ip, port);
            Listener.Start(BACKLOG_SIZE);
            Listener.BeginAcceptSocket(OnNewClientConnect, null);
            Port = port;
        }
        private void OnNewClientConnect(IAsyncResult iar)
        {
            if (Disposed)
                return;
            Socket clientsocket;
            try
            {
                clientsocket = Listener.EndAcceptSocket(iar);
            }
            catch(Exception e)
            {
                if (!Disposed)
                    Listener.BeginAcceptSocket(OnNewClientConnect, null);
                ServerConsole.Error(String.Format("EndAccept Server Error: {0}", e));
                return; //Intentional, we have to scap this connection but still let the listener survive.
            }

            try
            {
                Listener.BeginAcceptSocket(OnNewClientConnect, null);
                if (OnClientConnected != null)
                {
                    OnClientConnected(clientsocket);
                }
            }
            catch(Exception ex)
            {
                ServerConsole.Error(String.Format("Server Error: {0}", ex));
            }
        }
    }
}
