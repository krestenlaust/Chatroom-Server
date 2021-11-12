using System;
using System.Net.Sockets;
using System.Text;

namespace ChatroomServer.ClientPackets
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

            byte[] lengthBytes = new byte[sizeof(ushort)];
            stream.Read(lengthBytes, 0, sizeof(ushort));
            ushort length = BitConverter.ToUInt16(lengthBytes, 0);

            byte[] messageBytes = new byte[length];
            stream.Read(messageBytes, 0, length);
            Message = Encoding.UTF8.GetString(messageBytes, 0, length);
        }
    }
}
