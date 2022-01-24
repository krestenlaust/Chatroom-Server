using ChatroomServer.EventArguments;

namespace ChatroomServer
{
    /// <summary>
    /// Interface recognized as server feature.
    /// Called when a packet has been received.
    /// </summary>
    public interface IPacketReceived
    {
        /// <summary>
        /// Called when a packet has been received.
        /// </summary>
        /// <param name="sender">The server context.</param>
        /// <param name="e">Event arguments.</param>
        public void OnPacketReceived(ServerContext sender, PacketReceivedEventArgs e);
    }
}
