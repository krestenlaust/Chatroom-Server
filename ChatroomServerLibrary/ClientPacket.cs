using System.Net.Sockets;

namespace ChatroomServer
{
    /// <summary>
    /// The types of packets the server recieves from the clients.
    /// </summary>
    public enum ClientPacketType : byte
    {
        SendMessage = 2,
        ChangeName = 4,
        Disconnect = 10
    }

    public abstract class ClientPacket
    {
        public ClientPacketType PacketType { get; protected set; }

        public ClientPacket(NetworkStream stream) {}
    }
}
