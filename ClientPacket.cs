using System;
using System.Collections.Generic;
using System.Text;

namespace ChatrumServer
{
    public enum ClientPacketType : byte
    {

    }

    public abstract class ClientPacket
    {
        public ClientPacketType PacketType { get; protected set; }

        public abstract void Parse(byte[] data);
    }
}
