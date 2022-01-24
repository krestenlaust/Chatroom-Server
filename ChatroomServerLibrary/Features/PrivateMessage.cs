using ChatroomServer.EventArguments;

namespace ChatroomServer.Features
{
    public class PrivateMessage : IPacketReceived
    {
        public void OnPacketReceived(ServerContext sender, PacketReceivedEventArgs e)
        {
            if (e.PacketType != ClientPacketType.SendMessage)
            {
                return;
            }

            // TODO: implement private messages
        }
    }
}
