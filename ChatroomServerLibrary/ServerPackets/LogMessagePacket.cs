using System;
using System.Collections.Generic;
using System.Text;

namespace ChatroomServer.Packets
{
    public class LogMessagePacket : ServerPacket
    {
        public int Timestamp { get; private set; }
        public string Message { get; private set; }

        public LogMessagePacket(int timestamp, string message)
        {
            PacketType = ServerPacketType.LogMessage;

            Timestamp = timestamp;
            Message = message;
        }

        public override byte[] Serialize()
        {
            byte[] messageBytes = Message.SerializeAndPrependLengthUshort();

            byte[] bytes = new byte[1 + sizeof(int) + messageBytes.Length];

            int cur = 0;
            bytes[cur++] = (byte)PacketType;
            
            BitConverter.GetBytes(Timestamp).CopyTo(bytes, cur);
            cur += sizeof(int);

            messageBytes.CopyTo(bytes, cur);
            cur += messageBytes.Length;

            return bytes;
        }
    }
}
