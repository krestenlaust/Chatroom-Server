using System;
using System.Collections.Generic;
using System.Text;

#nullable enable
namespace ChatroomServer.Packets
{
    public class ReceiveMessagePacket : ServerPacket
    {
        public bool PrivateMessage { get; private set; }

        public byte UserID { get; private set; }

        public long Timestamp { get; private set; }

        public string Message { get; private set; }

        public ReceiveMessagePacket(byte userid, bool privateMessage, long timestamp, string message)
        {
            PacketType = ServerPacketType.ReceiveMessage;

            UserID = userid;
            PrivateMessage = privateMessage;
            Timestamp = timestamp;
            Message = message;
        }

        public override byte[] Serialize()
        {
            PacketBuilder builder = new PacketBuilder(
                sizeof(ServerPacketType) +
                sizeof(bool) +
                sizeof(byte) +
                sizeof(long) +
                sizeof(ushort) +
                Encoding.UTF8.GetByteCount(Message));

            builder.AddByte((byte)PacketType);

            builder.AddBool(PrivateMessage);

            builder.AddByte(UserID);

            builder.AddInt64(Timestamp);

            builder.AddUInt16((ushort)Encoding.UTF8.GetByteCount(Message));
            builder.AddStringUTF8(Message);

            return builder.Data;
        }
    }
}
