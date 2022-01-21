namespace ChatroomServer.Packets
{
    public class PingPacket : ServerPacket
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PingPacket"/> class.
        /// </summary>
        public PingPacket()
        {
            PacketType = ServerPacketType.Ping;
        }

        /// <inheritdoc/>
        public override byte[] Serialize()
        {
            return new byte[] { (byte)PacketType };
        }
    }
}
