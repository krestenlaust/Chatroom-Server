using ChatroomServer.ClientPackets;
using ChatroomServer.Packets;
using ChatroomServer.ServerPackets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;

namespace ChatroomServer
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

        private bool TryGetNextUserID(out byte userID)
        {
            userID = unchecked(currentUserID++);

            if (userID == 0)
            {
                return false;
            }

            return true;
        }

        private long GetUnixTime() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        private void OnClientConnect(IAsyncResult ar)
        {
            TcpClient client = tcpListener.EndAcceptTcpClient(ar);

            NetworkStream stream = client.GetStream();

            byte userID;
            if (!TryGetNextUserID(out userID))
            {
                client.Close();
                Console.WriteLine("Lobby full");
                return;
            }

            SendUserIDPacket userIDPacket = new SendUserIDPacket(userID);

            stream.Write(userIDPacket.Serialize());

            ClientInfo clientInfo = new ClientInfo(client, GetUnixTime());
            clients.Add(userID, clientInfo);

            Console.WriteLine($"{client.Client.RemoteEndPoint} connected: ID {userID}");

            tcpListener.BeginAcceptTcpClient(new AsyncCallback(OnClientConnect), null);
        }

        public void Update()
        {
            // Pings clients
            foreach (var client in clients)
            {
                // Ping client if more time than whats good, has passed.
                long millisecondsDifference = GetUnixTime() - client.Value.LastActiveTime;
                if (millisecondsDifference <= MaxMillisecondsSinceLastActive)
                {
                    Console.WriteLine($"Client: {millisecondsDifference} ms since last message");
                    continue;
                }

                if (client.Value.Name is null)
                {
                    Console.WriteLine("Clinet timed out: " + client.Key);
                    DisconnectClient(client.Key);
                    continue;
                }

                SendPacket(client.Key, client.Value, new PingPacket().Serialize());
            }

            // Check for new packets from clients and parses them
            foreach (var client in clients)
            {
                NetworkStream stream = client.Value.TcpClient.GetStream();
                if (!stream.DataAvailable)
                {
                    continue;
                }

                // Refresh last active.
                client.Value.LastActiveTime = GetUnixTime();

                ClientPacketType packetType = (ClientPacketType)stream.ReadByte();
                // Parse and handle the packets ChangeNamePacket and SendMessagePacket,
                // by responding to all other clients with a message or a userinfo update
                switch (packetType)
                {
                    case ClientPacketType.ChangeName:
                        ChangeNamePacket changeNamePacket = new ChangeNamePacket();
                        changeNamePacket.Parse(stream);

                        if (client.Value.Name is null)
                        {
                            foreach (var c in clients)
                            {
                                if (c.Value.Name is null)
                                {
                                    continue;
                                }

                                SendUserInfoPacket packet = new SendUserInfoPacket(c.Key, c.Value.Name);
                                byte[] packetBytes = packet.Serialize();
                                stream.Write(packetBytes, 0, packetBytes.Length);
                            }
                        }

                        // Change the name of the client
                        client.Value.Name = changeNamePacket.Name;

                        SendUserInfoPacket sendUserInfoPacket = new SendUserInfoPacket(client.Key, client.Value.Name);
                        SendPacketAll(sendUserInfoPacket.Serialize());
                        break;
                    case ClientPacketType.SendMessage:
                        SendMessagePacket sendMessagePacket = new SendMessagePacket();
                        sendMessagePacket.Parse(stream);

                        ReceiveMessagePacket receiveMessagePacket = new ReceiveMessagePacket(client.Key, GetUnixTime(), sendMessagePacket.Message);

                        if (sendMessagePacket.TargetUserID == 0)
                        {
                            SendPacketAll(receiveMessagePacket.Serialize());
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

        private void DisconnectClient(byte userID)
        {
            Console.WriteLine($"Disconnected ID: {userID}");

            clients[userID].TcpClient?.Close();

            // Remove client.
            clients.Remove(userID);

            // Send UserLeftPacket to all clients iteratively.
            SendPacketAll(new UserLeftPacket(userID).Serialize(), userID);
        }

        /// <summary>
        /// Returns whether the client is still connected.
        /// </summary>
        /// <param name="client"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        private void SendPacket(byte userID, ClientInfo client, byte[] data)
        {
            client.LastActiveTime = GetUnixTime();
            
            try
            {
                NetworkStream stream = client.TcpClient.GetStream();
                stream.Write(data, 0, data.Length);
            }
            catch (IOException)
            {
                // Disconnect client because it isn't connected.
                DisconnectClient(userID);
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

                if (client.Value.Name is null)
                {
                    continue;
                }

                SendPacket(client.Key, client.Value, data);
            }
        }
    }
}
