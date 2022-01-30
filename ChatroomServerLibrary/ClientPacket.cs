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
        TellName = 4,
        Disconnect = 10,
    }

    /// <summary>
    /// The most general class for client packet parsing.
    /// </summary>
    public abstract class ClientPacket : Packet<ClientPacketType>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ClientPacket"/> class.
        /// </summary>
        /// <param name="stream">Enforces design.</param>
        public ClientPacket(NetworkStream stream)
        {
        }
    }
}
