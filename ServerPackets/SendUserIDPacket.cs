using System;
using System.Collections.Generic;
using System.Text;

namespace ChatrumServer.Packets
{
    public class SendUserIDPacket : ServerPacket
    {
        public byte UserID { get; private set; }

        public SendUserIDPacket(byte userid)
        {
            PacketType = ServerPacketType.SendUserID;

            UserID = userid;
        }

        public override byte[] Serialize()
        {
            return new byte[] {
                (byte)PacketType,
                UserID
            };
        }
    }
}
