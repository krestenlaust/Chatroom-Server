﻿using System;
using System.Collections.Generic;
using System.Text;

namespace ChatroomServer.Packets
{
    public class PingPacket : ServerPacket
    {
        public PingPacket()
        {
            PacketType = ServerPacketType.Ping;
        }

        public override byte[] Serialize()
        {
            return new byte[] { (byte)PacketType };
        }
    }
}