using System;
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
        public readonly TcpClient TcpClient;

        /// <summary>
        /// Field is null when the client hasn't sent their name.
        /// </summary>
        public string? Name;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClientInfo"/> class with last active time set to now.
        /// </summary>
        /// <param name="tcpClient">Connection to client.</param>
        public ClientInfo(TcpClient tcpClient)
        {
            TcpClient = tcpClient;
            LastActiveUTCTime = DateTime.UtcNow;
        }

        /// <summary>
        /// Gets when something was last sent to the client, or recieved from the client.
        /// </summary>
        public DateTime LastActiveUTCTime { get; private set; }

        public void UpdateLastActiveTime() => LastActiveUTCTime = DateTime.UtcNow;
    }
}
