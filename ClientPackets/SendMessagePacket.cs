using System;
using System.Collections.Generic;
using System.Text;

namespace ChatrumServer.ClientPackets
{
    public class SendMessagePacket : ClientPacket
    {
        public byte TargetUserID { get; private set; }
        public string Message { get; private set; }

        public override void Parse(byte[] data)
        {
            TargetUserID = data[0];

            int length = BitConverter.ToInt32(data, 1);
            Message = Encoding.UTF8.GetString(data, 5, length);
        }
    }
}
