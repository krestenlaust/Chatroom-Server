using ChatroomServer.EventArguments;

namespace ChatroomServer.Features
{
    /// <summary>
    /// Plugin class for sending the message of the day.
    /// </summary>
    public class MessageOfTheDay : IHandshakeFinished
    {
        /// <inheritdoc/>
        public void OnHandshakeFinished(ServerContext sender, HandshakeFinishedEventArgs e)
        {
            if (sender.Config.MessageOfTheDay is null)
            {
                return;
            }

            e.Client.SendServerLog(sender.Config.MessageOfTheDay);
        }
    }
}
