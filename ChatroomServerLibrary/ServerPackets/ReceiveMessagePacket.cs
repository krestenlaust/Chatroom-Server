using System.Text;

#nullable enable
namespace ChatroomServer.ServerPackets
{
    public class ReceiveMessagePacket : TimestampedPacket
    {
        /// <summary>
        /// Gets 0 if public, otherwise the target user.
        /// </summary>
        public readonly byte TargetUserID;

        public readonly byte UserID;

        public readonly string Message;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReceiveMessagePacket"/> class.
        /// </summary>
        /// <param name="userid"></param>
        /// <param name="targetID"></param>
        /// <param name="message"></param>
        public ReceiveMessagePacket(byte userid, byte targetID, string message)
        {
            PacketType = ServerPacketType.ReceiveMessage;

            UserID = userid;
            TargetUserID = targetID;
            Message = message;
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
                sizeof(long) +
                sizeof(ushort) +
                Encoding.UTF8.GetByteCount(Message));

            builder.AddByte((byte)PacketType);

            builder.AddByte(TargetUserID);

            builder.AddByte(UserID);

            builder.AddInt64(Timestamp);

            builder.AddUInt16((ushort)Encoding.UTF8.GetByteCount(Message));
            builder.AddStringUTF8(Message);

            SerializedData = builder.Data;
            return builder.Data;
        }
    }
}
