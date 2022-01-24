namespace ChatroomServer.ServerPackets
{
    public class SendUserIDPacket : ServerPacket
    {
        public readonly byte UserID;

        /// <summary>
        /// Initializes a new instance of the <see cref="SendUserIDPacket"/> class.
        /// </summary>
        /// <param name="userid"></param>
        public SendUserIDPacket(byte userid)
        {
            PacketType = ServerPacketType.SendUserID;

            UserID = userid;
        }

        /// <inheritdoc/>
        public override byte[] Serialize()
        {
            return new byte[]
            {
                (byte)PacketType,
                UserID,
            };
        }
    }
}
