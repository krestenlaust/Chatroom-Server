﻿using System;
using System.Net.Sockets;
using System.Text;

namespace ChatroomServer.ClientPackets
{
    public class ChangeNamePacket : ClientPacket
    {
        public string Name { get; private set; }

        public ChangeNamePacket()
        {
            PacketType = ClientPacketType.ChangeName;
        }

        public override void Parse(NetworkStream stream)
        {
            byte length = (byte)stream.ReadByte();

            byte[] nameBytes = new byte[length];
            stream.Read(nameBytes, 0, length);
            Name = Encoding.UTF8.GetString(nameBytes, 0, length);
        }
    }
}
