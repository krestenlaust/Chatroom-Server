using System;
using System.Net.Sockets;
using System.Text;

namespace ChatrumServer.ClientPackets
{
    public class ChangeNamePacket : ClientPacket
    {
        public string Name { get; private set; }

        public ChangeNamePacket()
        {
            PacketType = ClientPacketType.ChangeName;
        }

        public override void Parse(NetworkStream stream)
        {
            byte[] lengthBytes = new byte[sizeof(int)];
            stream.Read(lengthBytes, 0, sizeof(int));
            int length = BitConverter.ToInt32(lengthBytes, 0);

            byte[] nameBytes = new byte[length];
            stream.Read(nameBytes, 0, length);
            Name = Encoding.UTF8.GetString(nameBytes, 0, length);
        }
    }
}
