using System.Net.Sockets;
using System.Text;

namespace ChatroomServer.ClientPackets
{
    public class ChangeNamePacket : ClientPacket
    {
        public string Name { get; private set; }

        public ChangeNamePacket(NetworkStream stream) : base(stream)
        {
            PacketType = ClientPacketType.ChangeName;

            byte length = (byte)stream.ReadByte();

            if (length == 0)
            {
                Name = "{Unspecified}";
                return;
            }

            byte[] nameBytes = new byte[length];
            stream.Read(nameBytes, 0, length);
            Name = Encoding.UTF8.GetString(nameBytes, 0, length);
        }
    }
}
