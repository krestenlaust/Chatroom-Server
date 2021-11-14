using System;
using System.Text;

#nullable enable
namespace ChatroomServer
{
    public static class SerializationHelper
    {
        public static byte[] SerializeAndPrependLengthUshort(string message)
        {
            byte[] messageBytes = Encoding.UTF8.GetBytes(message);
            byte[] data = new byte[sizeof(ushort) + messageBytes.Length];

            // Copy length to data.
            BitConverter.GetBytes((ushort)messageBytes.Length).CopyTo(data, 0);

            // Copy message to data.
            messageBytes.CopyTo(data, sizeof(ushort));

            return data;
        }

        public static byte[] SerializeAndPrependLengthByte(string message)
        {
            byte[] messageBytes = Encoding.UTF8.GetBytes(message);
            byte[] data = new byte[sizeof(byte) + messageBytes.Length];

            // Write length to data.
            data[0] = (byte)messageBytes.Length;

            // Write message to data.
            messageBytes.CopyTo(data, sizeof(byte));

            return data;
        }
    }
}
