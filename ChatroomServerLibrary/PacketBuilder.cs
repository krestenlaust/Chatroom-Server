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

        public byte[] Data => packet;

        /// <summary>
        /// Initializes a new instance of the <see cref="PacketBuilder"/> struct.
        /// </summary>
        /// <param name="size">Packet size.</param>
        public PacketBuilder(int size)
        {
            packet = new byte[size];
            packetCursor = 0;
        }

        /// <summary>
        /// Appends byte array.
        /// </summary>
        /// <param name="bytes"></param>
        public void AddBytes(byte[] bytes)
        {
            for (int i = 0; i < bytes.Length; i++)
            {
                packet[packetCursor] = bytes[i];
                packetCursor++;
            }
        }

        /// <summary>
        /// Appends byte.
        /// </summary>
        /// <param name="byteValue"></param>
        public void AddByte(byte byteValue)
        {
            packet[packetCursor] = byteValue;
            packetCursor++;
        }

        /// <summary>
        /// Appends bool represented as byte.
        /// </summary>
        /// <param name="value"></param>
        public void AddBool(bool value) => AddByte(BitConverter.GetBytes(value)[0]);

        /// <summary>
        /// Appends unsigned 16-bit integer.
        /// </summary>
        /// <param name="value"></param>
        public void AddUInt16(ushort value) => AddBytes(BitConverter.GetBytes(value));

        /// <summary>
        /// Appends signed 32-bit integer.
        /// </summary>
        /// <param name="value"></param>
        public void AddInt32(int value) => AddBytes(BitConverter.GetBytes(value));

        /// <summary>
        /// Appends signed 64-bit integer.
        /// </summary>
        /// <param name="value"></param>
        public void AddInt64(long value) => AddBytes(BitConverter.GetBytes(value));

        /// <summary>
        /// Appends UTF-8 encoded string represented as bytes.
        /// </summary>
        /// <param name="value"></param>
        public void AddStringUTF8(string value) => AddBytes(Encoding.UTF8.GetBytes(value));
    }
}
