using System;
using System.Collections.Generic;
using System.Text;

namespace ChatroomServer
{
    public static class ExtensionMethods
    {
        public static byte[] SerializeAndPrependLengthUshort(this string message)
        {
            byte[] messageBytes = Encoding.Default.GetBytes(message);
            byte[] bytes = new byte[sizeof(ushort) + messageBytes.Length];

            BitConverter.GetBytes((ushort)messageBytes.Length).CopyTo(bytes, 0);
            messageBytes.CopyTo(bytes, sizeof(ushort));

            return bytes;
        }

        public static byte[] SerializeAndPrependLengthByte(this string message)
        {
            byte[] messageBytes = Encoding.Default.GetBytes(message);
            byte[] bytes = new byte[1 + messageBytes.Length];

            bytes[0] = (byte)messageBytes.Length;
            messageBytes.CopyTo(bytes, sizeof(byte));

            return bytes;
        }
    }
}
