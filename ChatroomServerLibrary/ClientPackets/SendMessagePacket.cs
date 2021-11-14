using System;
using System.Net.Sockets;
using System.Text;

#nullable enable
namespace ChatroomServer.ClientPackets
{
    public class SendMessagePacket : ClientPacket
    {
        public byte TargetUserID { get; private set; }
        public string Message { get; private set; }

        public SendMessagePacket(NetworkStream stream) : base(stream)
        {
            PacketType = ClientPacketType.SendMessage;

            // Aflæs bruger ID
            TargetUserID = (byte)stream.ReadByte();

            // Aflæs længde
            byte[] lengthBytes = new byte[sizeof(ushort)];
            stream.Read(lengthBytes, 0, sizeof(ushort));
            ushort length = BitConverter.ToUInt16(lengthBytes, 0);

            if (length == 0)
            {
                Message = "";
                return;
            }

            byte[] messageBytes = new byte[length];
            stream.Read(messageBytes, 0, length);
            Message = Encoding.UTF8.GetString(messageBytes, 0, length);
        }
    }
}
