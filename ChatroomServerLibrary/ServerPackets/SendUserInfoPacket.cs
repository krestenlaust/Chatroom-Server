using System.Text;

#nullable enable
namespace ChatroomServer.ServerPackets
{
    public class SendUserInfoPacket : ServerPacket
    {
        public readonly byte UserID;

        public readonly string Name;

        /// <summary>
        /// Initializes a new instance of the <see cref="SendUserInfoPacket"/> class.
        /// </summary>
        /// <param name="userID"></param>
        /// <param name="name"></param>
        public SendUserInfoPacket(byte userID, string name)
        {
            PacketType = ServerPacketType.SendUserInfo;

            UserID = userID;
            Name = name;
        }

        /// <inheritdoc/>
        public override byte[] Serialize()
        {
            if (!(SerializedData is null))
            {
                return SerializedData;
            }

            PacketBuilder builder = new PacketBuilder(
                sizeof(ServerPacketType) +
                sizeof(byte) +
                sizeof(byte) +
                Encoding.UTF8.GetByteCount(Name));

            builder.AddByte((byte)PacketType);
            builder.AddByte(UserID);

            builder.AddByte((byte)Encoding.UTF8.GetByteCount(Name));
            builder.AddStringUTF8(Name);

            SerializedData = builder.Data;
            return builder.Data;
        }
    }
}
