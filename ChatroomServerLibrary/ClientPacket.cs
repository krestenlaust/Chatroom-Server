using System.Net.Sockets;

namespace ChatroomServer
{
    /// <summary>
    /// The types of packets the server recieves from the clients.
    /// </summary>
    public enum ClientPacketType : byte
    {
        Ping = 1,
        SendMessage = 2,
        ChangeName = 4,
        Disconnect = 10,
    }

    public abstract class ClientPacket : Packet<ClientPacketType>
    {
        public ClientPacket(NetworkStream stream)
        {
        }
    }
}
