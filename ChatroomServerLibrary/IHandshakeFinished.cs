using ChatroomServer.EventArguments;

namespace ChatroomServer
{
    /// <summary>
    /// Interface recognized as server feature.
    /// Called when a client turns into a user.
    /// </summary>
    public interface IHandshakeFinished
    {
        /// <summary>
        /// Called when a client turns into a user.
        /// </summary>
        /// <param name="sender">The server context.</param>
        /// <param name="e">Event arguments.</param>
        public void OnHandshakeFinished(ServerContext sender, HandshakeFinishedEventArgs e);
    }
}
