using System.Net.Sockets;

namespace LeattyServer.ServerInfo.Packets
{
    public class SocketInfo
    {
        public SocketInfo(Socket socket, short headerLength)
            : this(socket, headerLength, false)
        {
        }

        public SocketInfo(Socket socket, short headerLength, bool noEncryption)
        {
            Socket = socket;
            State = StateEnum.Header;
            NoEncryption = noEncryption;
            DataBuffer = new byte[headerLength];
            Index = 0;
        }

        public readonly Socket Socket;

        public bool NoEncryption;

        public StateEnum State;

        public byte[] DataBuffer;

        public int Index;

        public enum StateEnum { Header, Content }
    }
}
