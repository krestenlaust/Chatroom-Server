using System;

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
            byte[] messageBytes = SerializationHelper.SerializeAndPrependLengthUshort(Message);

            byte[] bytes = new byte[1 + sizeof(long) + messageBytes.Length];

            int cur = 0;
            bytes[cur++] = (byte)PacketType;
            
            BitConverter.GetBytes(Timestamp).CopyTo(bytes, cur);
            cur += sizeof(long);

            messageBytes.CopyTo(bytes, cur);
            cur += messageBytes.Length;

            return bytes;
        }
    }
}
