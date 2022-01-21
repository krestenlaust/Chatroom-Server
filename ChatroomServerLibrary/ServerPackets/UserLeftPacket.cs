#nullable enable
namespace ChatroomServer.ServerPackets
{
    public class UserLeftPacket : ServerPacket
    {
        public readonly byte UserID;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserLeftPacket"/> class.
        /// </summary>
        /// <param name="userID"></param>
        public UserLeftPacket(byte userID)
        {
            PacketType = ServerPacketType.UserLeft;

            UserID = userID;
        }

        /// <inheritdoc/>
        public override byte[] Serialize()
        {
            return new byte[]
            {
                (byte)PacketType,
                UserID,
            };
        }
    }
}
