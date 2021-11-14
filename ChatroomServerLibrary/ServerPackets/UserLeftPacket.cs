using System;
using System.Collections.Generic;
using System.Text;

#nullable enable
namespace ChatroomServer.ServerPackets
{
    public class UserLeftPacket : ServerPacket
    {
        public byte UserID { get; private set; }

        public UserLeftPacket(byte userID)
        {
            PacketType = ServerPacketType.UserLeft;

            UserID = userID;
        }

        public override byte[] Serialize()
        {
            return new byte[]
            {
                (byte)PacketType,
                UserID
            };
        }
    }
}
