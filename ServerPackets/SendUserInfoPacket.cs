using System;
using System.Collections.Generic;
using System.Text;

namespace ChatrumServer.Packets
{
    public class SendUserInfoPacket : ServerPacket
    {
        public byte UserID { get; private set; }
        public string Name { get; private set; }

        public SendUserInfoPacket(byte userID, string name)
        {
            PacketType = ServerPacketType.SendUserInfo;

            UserID = userID;
            Name = name;
        }

        public override byte[] Serialize()
        {
            byte[] nameBytes = Name.SerializeAndPrependLength();
            byte[] bytes = new byte[1 + 1 + nameBytes.Length];

            int cur = 0;
            bytes[cur++] = (byte)PacketType;
            bytes[cur++] = UserID;

            nameBytes.CopyTo(bytes, cur);
            cur += nameBytes.Length;

            return bytes;
        }
    }
}
