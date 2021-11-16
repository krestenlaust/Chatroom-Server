using System;
using System.Collections.Generic;
using System.Text;

#nullable enable
namespace ChatroomServer.Packets
{
    public class ReceiveMessagePacket : ServerPacket
    {
        public byte UserID { get; private set; }

        public long Timestamp { get; private set; }

        public string Message { get; private set; }

        public ReceiveMessagePacket(byte userid, long timestamp, string message)
        {
            PacketType = ServerPacketType.ReceiveMessage;

            UserID = userid;
            Timestamp = timestamp;
            Message = message;
        }

        public override byte[] Serialize()
        {
            PacketBuilder builder = new PacketBuilder(
                sizeof(ServerPacketType) +
                sizeof(byte) +
                sizeof(long) +
                sizeof(ushort) +
                Encoding.UTF8.GetByteCount(Message));

            builder.AddByte((byte)PacketType);

            builder.AddByte(UserID);

            builder.AddInt64(Timestamp);

            builder.AddUInt16((ushort)Encoding.UTF8.GetByteCount(Message));
            builder.AddStringUTF8(Message);

            return builder.Data;
        }
    }
}
