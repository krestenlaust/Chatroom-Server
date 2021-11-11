using System;
using System.Collections.Generic;
using System.Text;

namespace ChatrumServer.ClientPackets
{
    public class ChangeNamePacket : ClientPacket
    {
        public string Message { get; private set; }

        public override void Parse(byte[] data)
        {
            int length = BitConverter.ToInt32(data, 0);
            Message = Encoding.UTF8.GetString(data, 4, length);
        }
    }
}
