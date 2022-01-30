using System.Net.Sockets;
using System.Text;

namespace ChatroomServer.ClientPackets
{
    public class TellNamePacket : ClientPacket
    {
        public string Name { get; private set; }

        public TellNamePacket(NetworkStream stream) : base(stream)
        {
            PacketType = ClientPacketType.TellName;

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
