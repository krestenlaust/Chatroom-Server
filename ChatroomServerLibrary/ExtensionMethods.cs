using System;
using System.Collections.Generic;
using System.Text;

namespace ChatroomServer
{
    public static class ExtensionMethods
    {
        public static byte[] SerializeAndPrependLength(this string message)
        {
            byte[] messageBytes = Encoding.Default.GetBytes(message);
            byte[] bytes = new byte[sizeof(int) + messageBytes.Length];

            BitConverter.GetBytes(messageBytes.Length).CopyTo(bytes, 0);
            messageBytes.CopyTo(bytes, 4);

            return bytes;
        }
    }
}
