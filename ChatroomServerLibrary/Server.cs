using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using ChatroomServer.ClientPackets;
using ChatroomServer.Packets;
using ChatroomServer.ServerPackets;

#nullable enable
namespace ChatroomServer
{
    /// <summary>
    /// A chatroom server instance.
    /// </summary>
    public class Server : IDisposable
    {
        public Logger? Logger;
        private readonly Dictionary<byte, ClientInfo> clients = new Dictionary<byte, ClientInfo>();
        private readonly Dictionary<byte, TcpClient> newClients = new Dictionary<byte, TcpClient>();
        private readonly TcpListener tcpListener;
        private readonly ServerConfig config;
        private readonly Queue<(string Name, ReceiveMessagePacket StoredMessage)> recallableMessages = new Queue<(string Name, ReceiveMessagePacket StoredMessage)>();

        /// <summary>
        /// Initializes a new instance of the <see cref="Server"/> class.
        /// </summary>
        /// <param name="port">The port on which to start the server.</param>
        /// <param name="config">The configuration of the server.</param>
        public Server(short port, ServerConfig config)
        {
            tcpListener = new TcpListener(System.Net.IPAddress.Any, port);
            this.config = config;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Server"/> class.
        /// </summary>
        /// <param name="port">The port on which to start the server.</param>
        /// <param name="config">The configuration of the server.</param>
        /// <param name="logger">The logger with which to log information.</param>
        public Server(short port, ServerConfig config, Logger logger)
        {
            tcpListener = new TcpListener(System.Net.IPAddress.Any, port);
            this.Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.config = config;
        }

        /// <summary>
        /// Starts the server. Should only be called once.
        /// </summary>
        /// <exception cref="SocketException">Port unavailable, or otherwise.</exception>
        public void Start()
        {
            tcpListener.Start();
            tcpListener.BeginAcceptTcpClient(new AsyncCallback(OnClientConnect), null);
        }

        public void Dispose()
        {
            foreach (var item in clients)
            {
                item.Value.TcpClient?.Dispose();
            }
        }

        /// <summary>
        /// Reads available data from clients, and responds accordingly.
        /// Should be called repeatedly.
        /// </summary>
        public void Update()
        {
            // Add new clients
            foreach (var newClient in newClients)
            {
                ClientInfo newClientInfo = new ClientInfo(newClient.Value, GetUnixTime());

                clients.Add(newClient.Key, newClientInfo);
            }

            newClients.Clear();

            // Pings clients
            foreach (var client in clients)
            {
                // Ping client if more time than whats good, has passed.
                long timeDifference = GetUnixTime() - client.Value.LastActiveTime;
                if (timeDifference <= config.MaxTimeSinceLastActive)
                {
                    Logger?.Debug($"Client: {timeDifference} ms since last message");
                    continue;
                }

                if (client.Value.Name is null)
                {
                    Logger?.Info("Client timed out: " + client.Key);
                    DisconnectClient(client.Key);
                    continue;
                }

                SendPacket(client.Key, client.Value, new PingPacket().Serialize());
            }

            // Check for new packets from clients and parses them
            foreach (var client in clients)
            {
                try
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
                            var changeNamePacket = new ChangeNamePacket(stream);

                            // First time connecting
                            if (client.Value.Name is null)
                            {
                                IntroduceClient(client.Key, client.Value);

                                Logger?.Info("User connected: " + changeNamePacket.Name);
                                SendPacketAll(new LogMessagePacket(GetUnixTime(), $"{changeNamePacket.Name} has connected").Serialize());
                            }
                            else
                            {
                                Logger?.Info($"User {client.Key} name updated from {client.Value.Name} to {changeNamePacket.Name}");
                            }

                            // Change the name of the client
                            client.Value.Name = changeNamePacket.Name;

                            SendPacketAll(new SendUserInfoPacket(client.Key, client.Value.Name).Serialize());
                            break;
                        case ClientPacketType.SendMessage:
                            var sendMessagePacket = new SendMessagePacket(stream);

                            // Ignore packet if client handshake hasn't finished.
                            if (client.Value.Name is null)
                            {
                                break;
                            }

                            var responsePacket = new ReceiveMessagePacket(client.Key, sendMessagePacket.TargetUserID, GetUnixTime(), sendMessagePacket.Message);

                            // Debug display message.
                            if (!(Logger is null))
                            {
                                StringBuilder displayedMessage = new StringBuilder($"User {client.Key} messaged {sendMessagePacket.TargetUserID}: ");
                                const int minimumDisplayedMessage = 15;
                                int messageLength = Math.Min(Math.Max(Console.BufferWidth - displayedMessage.Length - 7, minimumDisplayedMessage), sendMessagePacket.Message.Length);
                                displayedMessage.Append(sendMessagePacket.Message.Substring(0, messageLength));
                                if (messageLength < sendMessagePacket.Message.Length)
                                {
                                    displayedMessage.Append("[...]");
                                }

                                Logger.Info(displayedMessage.ToString());
                            }

                            if (sendMessagePacket.TargetUserID == 0)
                            {
                                recallableMessages.Enqueue((client.Value.Name, responsePacket));

                                // Shorten recallable message queue to configuration
                                while (recallableMessages.Count > config.MaxStoredMessages)
                                {
                                    recallableMessages.Dequeue();
                                }

                                SendPacketAll(responsePacket.Serialize());
                            }
                            else
                            {
                                SendPacket(sendMessagePacket.TargetUserID, clients[sendMessagePacket.TargetUserID], responsePacket.Serialize());
                                SendPacket(client.Key, client.Value, responsePacket.Serialize());
                            }

                            break;
                        case ClientPacketType.Disconnect:
                            Logger?.Info($"Client: {client.Value.Name} has disconnected");
                            SendPacketAll(new LogMessagePacket(GetUnixTime(), $"{client.Value.Name} has disconnected").Serialize());
                            DisconnectClient(client.Key);
                            break;
                        default:
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Logger?.Error(ex.ToString());
                    throw;
                }
            }
        }

        private void IntroduceClient(byte id, ClientInfo client)
        {
            // Clone queue
            var messagesToRecall = new Queue<(string Name, ReceiveMessagePacket)>(recallableMessages);

            // Tuples .GetHashcode combines its element's hashcodes.
            HashSet<(byte, string)> updatedUserinfo = new HashSet<(byte, string)>();

            while (messagesToRecall.Count > 0)
            {
                (string authorname, ReceiveMessagePacket packet) = messagesToRecall.Dequeue();

                if (updatedUserinfo.Add((packet.UserID, authorname)))
                {
                    // Send UpdateUserInfo
                    SendPacket(id, client, new SendUserInfoPacket(packet.UserID, authorname).Serialize());
                }

                SendPacket(id, client, packet.Serialize());
            }

            // Send user ID's
            foreach (KeyValuePair<byte, ClientInfo> otherClientPair in clients)
            {
                byte otherClientID = otherClientPair.Key;
                string? otherClientName = otherClientPair.Value.Name;

                // A client that is just connecting.
                if (otherClientName is null)
                {
                    continue;
                }

                // Client name has already been sent.
                if (updatedUserinfo.Contains((otherClientID, otherClientName)))
                {
                    continue;
                }

                SendPacket(id, client, new SendUserInfoPacket(otherClientID, otherClientName).Serialize());
            }
        }

        private byte? GetNextUserID()
        {
            for (byte i = 1; i < byte.MaxValue; i++)
            {
                if (!clients.ContainsKey(i) && !newClients.ContainsKey(i))
                {
                    return i;
                }
            }

            return null;
        }

        private long GetUnixTime() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        /// <summary>
        /// På en anden tråd, pas på kald i denne metode.
        /// </summary>
        /// <param name="ar"></param>
        private void OnClientConnect(IAsyncResult ar)
        {
            // Begin listening for next.
            tcpListener.BeginAcceptTcpClient(new AsyncCallback(OnClientConnect), null);

            // Begin handling the connecting client.
            TcpClient client = tcpListener.EndAcceptTcpClient(ar);
            NetworkStream stream = client.GetStream();

            // If lobby is full, turn away client.
            if (!(GetNextUserID() is byte userID))
            {
                client.Close();
                Logger?.Warning("Lobby full");
                return;
            }

            // Assign client their ID
            stream.Write(new SendUserIDPacket(userID).Serialize());

            newClients.Add(userID, client);

            Logger?.Info($"{client.Client.RemoteEndPoint} connected: ID {userID}");
        }

        private void DisconnectClient(byte userID)
        {
            Logger?.Info($"Disconnected ID: {userID}");

            // Clients kan modificeres da den formegentlig er på den rigtige tråd.
            clients[userID].TcpClient?.Close();

            // Remove client.
            clients.Remove(userID);

            // Send UserLeftPacket to all clients iteratively.
            SendPacketAll(new UserLeftPacket(userID).Serialize(), userID);
        }

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
