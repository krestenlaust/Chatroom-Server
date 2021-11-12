using System;
using System.Collections.Generic;
using System.Text;

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
            byte[] timestampBytes = BitConverter.GetBytes(Timestamp);
            byte[] messageBytes = Message.SerializeAndPrependLength();

            byte[] bytes = new byte[1 + 1 + timestampBytes.Length + messageBytes.Length];
            int cur = 0;
            bytes[cur++] = (byte)PacketType;
            bytes[cur++] = UserID;
            timestampBytes.CopyTo(bytes, cur++);
            cur += timestampBytes.Length;

            messageBytes.CopyTo(bytes, cur);
            cur += messageBytes.Length;

            return bytes;
        }
    }
}
