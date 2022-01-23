using System;
using System.IO;
using System.Net.Sockets;

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
        /// Field is null when the client hasn't sent their name.
        /// </summary>
        public string? Name;

        /// <summary>
        /// Clients flagged as disconnected, will be removed.
        /// </summary>
        public bool Disconnected = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="Client"/> class with last active time set to now.
        /// </summary>
        /// <param name="tcpClient">Connection to client.</param>
        public Client(TcpClient tcpClient, byte ID)
        {
            TcpClient = tcpClient;
            LastActiveUTCTime = DateTime.UtcNow;
            this.ID = ID;
        }

        /// <summary>
        /// Gets when something was last sent to the client, or recieved from the client.
        /// </summary>
        public DateTime LastActiveUTCTime { get; private set; }

        /// <summary>
        /// Changes the LastActiveUTCTime to current UTC time.
        /// </summary>
        public void UpdateLastActiveTime() => LastActiveUTCTime = DateTime.UtcNow;

        public void SendPacket<T>(T packet)
            where T : ServerPacket
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
    }
}
