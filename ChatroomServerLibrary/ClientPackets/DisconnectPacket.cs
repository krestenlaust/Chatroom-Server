namespace ChatroomServer.ClientPackets
{
    /// <summary>
    /// Single-byte long packet used for describing.
    /// </summary>
    public class DisconnectPacket : ClientPacket
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DisconnectPacket"/> class.
        /// </summary>
        public DisconnectPacket() : base(null)
        {
        }
    }
}
