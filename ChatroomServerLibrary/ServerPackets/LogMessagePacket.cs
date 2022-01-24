using System.Text;

namespace ChatroomServer.ServerPackets
{
    public class LogMessagePacket : TimestampedPacket
    {
        public readonly string Message;

        /// <summary>
        /// Initializes a new instance of the <see cref="LogMessagePacket"/> class.
        /// </summary>
        /// <param name="message">The message field of the packet.</param>
        public LogMessagePacket(string message)
        {
            PacketType = ServerPacketType.LogMessage;

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
                sizeof(byte) +
                sizeof(long) +
                sizeof(ushort) +
                Encoding.UTF8.GetByteCount(Message));

            builder.AddByte((byte)PacketType);

            builder.AddInt64(Timestamp);

            builder.AddUInt16((ushort)Encoding.UTF8.GetByteCount(Message));
            builder.AddStringUTF8(Message);

            SerializedData = builder.Data;
            return builder.Data;
        }
    }
}
