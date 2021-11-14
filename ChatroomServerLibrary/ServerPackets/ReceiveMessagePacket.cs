﻿using System;
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
            byte[] timestampBytes = BitConverter.GetBytes(Timestamp);
            byte[] messageBytes = SerializationHelper.SerializeAndPrependLengthUshort(Message);

            byte[] bytes = new byte[1 + 1 + timestampBytes.Length + messageBytes.Length];
            int cur = 0;
            
            bytes[cur] = (byte)PacketType;
            cur += 1;

            bytes[cur] = UserID;
            cur += 1;

            timestampBytes.CopyTo(bytes, cur);
            cur += timestampBytes.Length;

            messageBytes.CopyTo(bytes, cur);
            cur += messageBytes.Length;

            return bytes;
        }
    }
}
