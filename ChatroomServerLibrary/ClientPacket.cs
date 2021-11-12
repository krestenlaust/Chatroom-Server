using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace ChatroomServer
{
    public enum ClientPacketType : byte
    {
        SendMessage = 2,
        ChangeName = 4
    }

    public abstract class ClientPacket
    {
        public ClientPacketType PacketType { get; protected set; }

        public abstract void Parse(NetworkStream stream);
    }
}
