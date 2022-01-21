using System;
using System.Collections.Generic;
using System.Text;

#nullable enable
namespace ChatroomServer.Packets
{
    public class ReceiveMessagePacket : ServerPacket
    {
        /// <summary>
        /// Gets 0 if public, otherwise the target user.
        /// </summary>
        public readonly byte TargetUserID;

        public readonly byte UserID;

        public readonly long Timestamp;

        public readonly string Message;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReceiveMessagePacket"/> class.
        /// </summary>
        /// <param name="userid"></param>
        /// <param name="targetID"></param>
        /// <param name="timestamp"></param>
        /// <param name="message"></param>
        public ReceiveMessagePacket(byte userid, byte targetID, long timestamp, string message)
        {
            PacketType = ServerPacketType.ReceiveMessage;

            UserID = userid;
            TargetUserID = targetID;
            Timestamp = timestamp;
            Message = message;
        }

        /// <inheritdoc/>
        public override byte[] Serialize()
        {
            if (!(serializedData is null))
            {
                return serializedData;
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

            serializedData = builder.Data;
            return builder.Data;
        }
    }
}
