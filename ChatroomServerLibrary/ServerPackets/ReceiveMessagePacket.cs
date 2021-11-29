using System;
using System.Collections.Generic;
using System.Text;

#nullable enable
namespace ChatroomServer.Packets
{
    public class ReceiveMessagePacket : ServerPacket
    {
        /// <summary>
        /// 0 if public.
        /// </summary>
        public byte TargetUserID { get; private set; }

        public byte UserID { get; private set; }

        public long Timestamp { get; private set; }

        public string Message { get; private set; }

        public ReceiveMessagePacket(byte userid, byte targetID, long timestamp, string message)
        {
            PacketType = ServerPacketType.ReceiveMessage;

            UserID = userid;
            TargetUserID = targetID;
            Timestamp = timestamp;
            Message = message;
        }

        public override byte[] Serialize()
        {
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

            return builder.Data;
        }
    }
}
