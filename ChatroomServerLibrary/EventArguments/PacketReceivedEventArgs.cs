using System;

#nullable enable
namespace ChatroomServer.EventArguments
{
    public class PacketReceivedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PacketReceivedEventArgs"/> class.
        /// </summary>
        /// <param name="client"></param>
        /// <param name="packet"></param>
        /// <param name="packetType"></param>
        /// <param name="responsePacket"></param>
        public PacketReceivedEventArgs(Client client, ClientPacket packet, ClientPacketType packetType, ServerPacket? responsePacket)
        {
            Client = client;
            Packet = packet;
            PacketType = packetType;
            ResponsePacket = responsePacket;
        }

        public Client Client { get; }

        public ClientPacket Packet { get; }

        public ServerPacket? ResponsePacket { get; }

        public ClientPacketType PacketType { get; }
    }
}
