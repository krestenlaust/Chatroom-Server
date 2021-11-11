using System;
using System.Collections.Generic;
using System.Text;

namespace ChatrumServer
{
    public enum ServerPacketType : byte
    {
        Ping = 1,
        ReceiveMessage = 3,
        LogMessage = 5,
        SendUserInfo = 7,
        SendUserID = 9,
        UserLeft = 11,
    }

    public abstract class ServerPacket
    {
        public ServerPacketType PacketType { get; protected set; }

        public abstract byte[] Serialize();
    }
}
