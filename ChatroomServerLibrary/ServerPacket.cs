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

    /// <summary>
    /// The most general class for serializing server packets.
    /// </summary>
    public abstract class ServerPacket : Packet<ServerPacketType>
    {
        /// <summary>
        /// Gets or sets stored serialized data.
        /// </summary>
        protected byte[]? SerializedData { get; set; } = null;

        /// <summary>
        /// Serializes the packet.
        /// </summary>
        /// <returns>Byte array of serialized data.</returns>
        public abstract byte[] Serialize();
    }
}
