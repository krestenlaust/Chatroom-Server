using System;
using System.Text;

namespace ChatroomServer.Packets
{
    public class LogMessagePacket : ServerPacket
    {
        public long Timestamp { get; private set; }

        public string Message { get; private set; }

        public LogMessagePacket(long timestamp, string message)
        {
            PacketType = ServerPacketType.LogMessage;

            Timestamp = timestamp;
            Message = message;
        }

        public override byte[] Serialize()
        {
            PacketBuilder builder = new PacketBuilder(
                sizeof(byte) +
                sizeof(long) +
                sizeof(ushort) +
                Encoding.UTF8.GetByteCount(Message));

            builder.AddByte((byte)PacketType);

            builder.AddInt64(Timestamp);

            builder.AddUInt16((ushort)Encoding.UTF8.GetByteCount(Message));
            builder.AddStringUTF8(Message);

            return builder.Data;
        }
    }
}
