using System.Net.Sockets;

#nullable enable
namespace ChatroomServer
{
    /// <summary>
    /// Keeps track of when to ping the client, it's name and the socket connected to the client.
    /// </summary>
    public class ClientInfo
    {
        /// <summary>
        /// The <c>TcpClient</c> belonging to a given client.
        /// </summary>
        public TcpClient TcpClient;

        /// <summary>
        /// Keeps track of when something was sent to the client, or recieved from the client.
        /// </summary>
        public long LastActiveTime;

        /// <summary>
        /// Field is null when the client hasn't sent their name.
        /// </summary>
        public string? Name;

        public ClientInfo(TcpClient tcpClient, long lastActiveTime)
        {
            TcpClient = tcpClient;
            LastActiveTime = lastActiveTime;
        }
    }
}
