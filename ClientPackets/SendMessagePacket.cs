using System;
using System.Net.Sockets;
using System.Text;

namespace ChatrumServer.ClientPackets
{
    public class SendMessagePacket : ClientPacket
    {
        public byte TargetUserID { get; private set; }
        public string Message { get; private set; }

        public SendMessagePacket()
        {
            PacketType = ClientPacketType.SendMessage;
        }

        public override void Parse(NetworkStream stream)
        {
            TargetUserID = (byte)stream.ReadByte();

            byte[] lengthBytes = new byte[sizeof(int)];
            stream.Read(lengthBytes, 0, sizeof(int));
            int length = BitConverter.ToInt32(lengthBytes, 0);

            byte[] messageBytes = new byte[length];
            stream.Read(messageBytes, 0, length);
            Message = Encoding.UTF8.GetString(messageBytes, 0, length);
        }
    }
}
