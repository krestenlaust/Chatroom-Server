using System;
using System.IO;
using System.Net.Sockets;
using ChatroomServer.ServerPackets;

#nullable enable
namespace ChatroomServer
{
    /// <summary>
    /// Keeps track of when to ping the client, its name and the socket connected to the client.
    /// </summary>
    public class Client
    {
        /// <summary>
        /// The <c>TcpClient</c> belonging to a given client.
        /// </summary>
        public readonly TcpClient TcpClient;

        /// <summary>
        /// The user ID.
        /// </summary>
        public readonly byte ID;

        /// <summary>
        /// Initializes a new instance of the <see cref="Client"/> class with last active time set to now.
        /// </summary>
        /// <param name="tcpClient">Connection to client.</param>
        /// <param name="ID">The unique ID given to the client.</param>
        public Client(TcpClient tcpClient, byte ID)
        {
            TcpClient = tcpClient;
            LastActiveUTCTime = DateTime.UtcNow;
            this.ID = ID;
        }

        /// <summary>
        /// Gets or sets the clients name.
        /// Is null when the client hasn't finished handshaking.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Gets when something was last sent to the client, or recieved from the client.
        /// </summary>
        public DateTime LastActiveUTCTime { get; private set; }

        /// <summary>
        /// Gets a value indicating whether a client should be removed as it's disconnected.
        /// </summary>
        internal bool Disconnected { get; private set; } = false;

        /// <summary>
        /// Changes the LastActiveUTCTime to current UTC time.
        /// </summary>
        public void UpdateLastActiveTime() => LastActiveUTCTime = DateTime.UtcNow;

        /// <summary>
        /// Sends a packet to the client.
        /// </summary>
        /// <param name="packet">The packet to send.</param>
        public void SendPacket(ServerPacket packet)
        {
            if (Disconnected)
            {
                return;
            }

            UpdateLastActiveTime();
            byte[] serializedPacket = packet.Serialize();

            try
            {
                NetworkStream stream = TcpClient.GetStream();
                stream.Write(serializedPacket, 0, serializedPacket.Length);
            }
            catch (Exception ex) when (ex is IOException || ex is InvalidOperationException)
            {
                // Disconnect client because it isn't connected.
                Disconnected = true;
            }
        }

        /// <summary>
        /// Creates a log packet and sends it to the client.
        /// </summary>
        /// <param name="serverMessage">The message to display.</param>
        public void SendServerLog(string serverMessage)
        {
            SendPacket(new LogMessagePacket(serverMessage));
        }
    }
}
