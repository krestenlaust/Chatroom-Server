using System;
using System.Collections.Generic;
using System.Text;

#nullable enable
namespace ChatroomServer.Packets
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

        /// <inheritdoc/>
        public override byte[] Serialize()
        {
            PacketBuilder builder = new PacketBuilder(
                sizeof(ServerPacketType) +
                sizeof(byte) +
                sizeof(byte) +
                Encoding.UTF8.GetByteCount(Name));

            builder.AddByte((byte)PacketType);
            builder.AddByte(UserID);

            builder.AddByte((byte)Encoding.UTF8.GetByteCount(Name));
            builder.AddStringUTF8(Name);

            return builder.Data;
        }
    }
}
