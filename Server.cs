using ChatrumServer.ClientPackets;
using ChatrumServer.Packets;
using ChatrumServer.ServerPackets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;

namespace ChatrumServer
{
    public class Server
    {
        private const int MaxMillisecondsSinceLastActive = 10000;
        private Dictionary<byte, ClientInfo> clients = new Dictionary<byte, ClientInfo>();
        private TcpListener tcpListener;
        private byte currentUserID = 1;

        public Server(short port)
        {
            tcpListener = new TcpListener(System.Net.IPAddress.Any, port);
        }   

        public void Start()
        {
            tcpListener.Start();
            tcpListener.BeginAcceptTcpClient(new AsyncCallback(OnClientConnect), null);
        }

        private void OnClientConnect(IAsyncResult ar)
        {
            TcpClient client = tcpListener.EndAcceptTcpClient(ar);

            NetworkStream stream = client.GetStream();

            byte userID = unchecked(currentUserID++);
            SendUserIDPacket userIDPacket = new SendUserIDPacket(userID);

            stream.Write(userIDPacket.Serialize());

            ClientInfo clientInfo = new ClientInfo(client, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
         
            clients.Add(userID, clientInfo);

            tcpListener.BeginAcceptTcpClient(new AsyncCallback(OnClientConnect), null);

            stream.BeginRead(clientInfo.Buffer, 0, clientInfo.Buffer.Length, new AsyncCallback(OnClientDataReceived), clientInfo);

            Console.WriteLine($"Client {userID} connected");
        }

        public void Update()
        {
            // Pings clients
            foreach (var client in clients)
            {
                // Ping client if more time than whats good, has passed.
                if (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - client.Value.TimeSinceActive > MaxMillisecondsSinceLastActive)
                {
                    SendPacket(client.Key, client.Value, new PingPacket().Serialize());
                }
            }

            // Check for new packets from clients and parses them
            foreach (var client in clients)
            {
                NetworkStream stream = client.Value.TcpClient.GetStream();
                if (!stream.DataAvailable)
                {
                    continue;
                }

                ClientPacketType packetType = (ClientPacketType)stream.ReadByte();
                // Parse and handle the packets ChangeNamePacket and SendMessagePacket,
                // by responding to all other clients with a message or a userinfo update
                switch (packetType)
                {
                    case ClientPacketType.ChangeName:
                        ChangeNamePacket changeNamePacket = new ChangeNamePacket();
                        changeNamePacket.Parse(stream);

                        // Change the name of the client
                        client.Value.Name = changeNamePacket.Name;

                        SendUserInfoPacket sendUserInfoPacket = new SendUserInfoPacket(client.Key, client.Value.Name);
                        SendPacketAll(sendUserInfoPacket.Serialize());
                        break;
                    case ClientPacketType.SendMessage:
                        SendMessagePacket sendMessagePacket = new SendMessagePacket();
                        sendMessagePacket.Parse(stream);

                        ReceiveMessagePacket receiveMessagePacket = new ReceiveMessagePacket(client.Key, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), sendMessagePacket.Message);

                        if (sendMessagePacket.TargetUserID == 0)
                        {
                            SendPacketAll(receiveMessagePacket.Serialize(), client.Key);
                        }
                        else
                        {
                            SendPacket(sendMessagePacket.TargetUserID, clients[sendMessagePacket.TargetUserID], receiveMessagePacket.Serialize());
                        }
                        break;
                    default:
                        break;
                }
            }
        }

        /// <summary>
        /// Returns whether the client is still connected.
        /// </summary>
        /// <param name="client"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        private void SendPacket(byte userID, ClientInfo client, byte[] data)
        {
            client.TimeSinceActive = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            
            try
            {
                NetworkStream stream = client.TcpClient.GetStream();
                stream.Write(data, 0, data.Length);
            }
            catch (IOException)
            {
                // Remove client.
                clients.Remove(userID);
                // Send UserLeftPacket to all clients iteratively.
                foreach (var clientInfo in clients)
                {
                    SendPacket(clientInfo.Key, clientInfo.Value, new UserLeftPacket(userID).Serialize());
                }
            }
        }

        private void SendPacketAll(byte[] data, byte exceptUser = 0)
        {
            foreach (var client in clients)
            {
                if (client.Key == exceptUser)
                {
                    continue;
                }

                SendPacket(client.Key, client.Value, data);
            }
        }
    }
}
