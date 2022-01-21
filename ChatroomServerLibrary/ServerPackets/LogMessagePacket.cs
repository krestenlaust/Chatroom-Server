using System;
using System.Text;

namespace ChatroomServer.Packets
{
    public class LogMessagePacket : ServerPacket
    {
        public readonly long Timestamp;

        public readonly string Message;

        /// <summary>
        /// Initializes a new instance of the <see cref="LogMessagePacket"/> class.
        /// </summary>
        /// <param name="timestamp"></param>
        /// <param name="message"></param>
        public LogMessagePacket(long timestamp, string message)
        {
            PacketType = ServerPacketType.LogMessage;

            Timestamp = timestamp;
            Message = message;
        }

        public override byte[] Serialize()
        {
            if (!(serializedData is null))
            {
                return serializedData;
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

            serializedData = builder.Data;
            return builder.Data;
        }
    }
}
