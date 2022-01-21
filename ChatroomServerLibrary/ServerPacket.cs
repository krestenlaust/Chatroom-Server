#nullable enable
namespace ChatroomServer
{
    /// <summary>
    /// The types of packets the server sends to the clients.
    /// </summary>
    public enum ServerPacketType : byte
    {
        Ping = 1,
        ReceiveMessage = 3,
        LogMessage = 5,
        SendUserInfo = 7,
        SendUserID = 9,
        UserLeft = 11,
    }

    public abstract class ServerPacket
    {
        /// <summary>
        /// Gets or sets the type of the packet.
        /// </summary>
        protected ServerPacketType PacketType { get; set; }

        protected byte[]? serializedData = null;

        /// <summary>
        /// Serializes the packet.
        /// </summary>
        /// <returns>Byte array of serialized data.</returns>
        public abstract byte[] Serialize();
    }
}
