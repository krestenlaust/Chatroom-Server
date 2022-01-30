using System.ComponentModel;
using ChatroomServer.ClientPackets;

namespace ChatroomServer.EventArguments
{
    public class InterceptIncomingMessageEventArgs : CancelEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InterceptIncomingMessageEventArgs"/> class.
        /// </summary>
        /// <param name="messagePacket"></param>
        public InterceptIncomingMessageEventArgs(SendMessagePacket messagePacket)
        {
            MessagePacket = messagePacket;
        }

        public SendMessagePacket MessagePacket { get; set; }
    }
}
