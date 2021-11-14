using System.Net.Sockets;

#nullable enable
namespace ChatroomServer
{
    public class ClientInfo
    {
        public TcpClient TcpClient;
        public long LastActiveTime;
        public string? Name;

        public ClientInfo(TcpClient tcpClient, long lastActiveTime)
        {
            TcpClient = tcpClient;
            LastActiveTime = lastActiveTime;
        }
    }
}
