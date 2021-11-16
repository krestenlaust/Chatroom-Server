using System;
using System.Collections.Generic;
using System.Text;

namespace ChatroomServer
{
    /// <summary>
    /// Helper struct for constructing packets.
    /// </summary>
    public struct PacketBuilder
    {
        private readonly byte[] packet;
        private ushort packetCursor;

        public byte[] Data
        {
            get => packet;
        }

        public PacketBuilder(int size)
        {
            packet = new byte[size];
            packetCursor = 0;
        }

        public void AddBytes(byte[] bytes)
        {
            for (int i = 0; i < bytes.Length; i++)
            {
                packet[packetCursor] = bytes[i];
                packetCursor++;
            }
        }

        public void AddByte(byte byteValue)
        {
            packet[packetCursor] = byteValue;
            packetCursor++;
        }

        public void AddUInt16(ushort value) => AddBytes(BitConverter.GetBytes(value));

        public void AddInt32(int value) => AddBytes(BitConverter.GetBytes(value));

        public void AddInt64(long value) => AddBytes(BitConverter.GetBytes(value));

        public void AddStringUTF8(string value) => AddBytes(Encoding.UTF8.GetBytes(value));
    }
}
