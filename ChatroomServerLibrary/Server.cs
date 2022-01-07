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
        private readonly HashSet<string> usedNames = new HashSet<string>();
        private readonly Dictionary<byte, ClientInfo> clients = new Dictionary<byte, ClientInfo>();
        private readonly Dictionary<byte, TcpClient> newClients = new Dictionary<byte, TcpClient>();
        private readonly TcpListener tcpListener;
        private readonly ServerConfig config;
        private readonly Queue<(string Name, ReceiveMessagePacket StoredMessage)> recallableMessages = new Queue<(string Name, ReceiveMessagePacket StoredMessage)>();

        private byte recentID = 1;

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
        }

        /// <inheritdoc/>
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
            // Check pending clients
            while (tcpListener.Pending())
            {
                // Accept pending client.
                TcpClient client = tcpListener.AcceptTcpClient();
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

                ClientInfo clientInfo = new ClientInfo(client, GetUnixTime());
                clients.Add(userID, clientInfo);

                Logger?.Info($"{client.Client.RemoteEndPoint} connected: ID {userID}");
            }

            newClients.Clear();

            // Pings clients
            foreach (var client in clients)
            {
                // Ping client if more time than whats good, has passed.
                long timeDifference = GetUnixTime() - client.Value.LastActiveTime;

                // Client hasn't handshaked yet.
                if (client.Value.Name is null)
                {
                    if (timeDifference <= config.HandshakeTimeout)
                    {
                        continue;
                    }

                    Logger?.Info($"Client handshake timed out: {client.Key}");
                    DisconnectClient(client.Key);

                    continue;
                }

                if (timeDifference <= config.MaxTimeSinceLastActive)
                {
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

                    byte packetID = (byte)stream.ReadByte();

                    if (!Enum.IsDefined(typeof(ClientPacketType), packetID))
                    {
                        // Unknown packet: Everything is fine!
                        Logger?.Warning($"Received unknown packet from {client.Key}");
                        continue;
                    }

                    var packetType = (ClientPacketType)packetID;

                    HandlePacket(stream, packetType, client.Key, client.Value);
                }
                catch (InvalidOperationException ex)
                {
                    Logger?.Warning($"Client disconnect by invalid operation: {ex}");
                    DisconnectClient(client.Key);
                }
                catch (SocketException ex)
                {
                    Logger?.Debug($"Client disconnected by Socket exception: {ex}");
                    DisconnectClient(client.Key);
                }
                catch (Exception ex)
                {
                    Logger?.Error(ex.ToString());
                    throw;
                }
            }
        }

        private bool IsNameValid(string name)
        {
            // TODO: Check length.
            // TODO: Check for invalid characters.

            // Is name taken
            if (usedNames.Contains(name))
            {
                return false;
            }

            return true;
        }

        private string FixName(string name)
        {
            string newName = name;
            int nextIndex = 2;
            while (!IsNameValid(newName))
            {
                newName = $"{name} ({nextIndex++})";
            }

            return newName;
        }

        /// <summary>
        /// Handles incoming packet.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="packetType"></param>
        /// <param name="clientID"></param>
        /// <param name="client"></param>
        private void HandlePacket(NetworkStream stream, ClientPacketType packetType, byte clientID, ClientInfo client)
        {
            // Parse and handle the packets ChangeNamePacket and SendMessagePacket,
            // by responding to all other clients with a message or a userinfo update
            switch (packetType)
            {
                case ClientPacketType.ChangeName:
                    var changeNamePacket = new ChangeNamePacket(stream);

                    string? oldName = client.Name;
                    client.Name = FixName(changeNamePacket.Name);
                    usedNames.Add(client.Name);

                    // First time connecting
                    if (oldName is null)
                    {
                        IntroduceClientToContext(clientID, client);

                        Logger?.Info("User connected: " + client.Name);
                        ServerLogAll($"{client.Name} forbandt!");
                    }
                    else
                    {
                        Logger?.Info($"User {clientID} name updated from {oldName} to {changeNamePacket.Name}");
                    }

                    SendPacketAll(new SendUserInfoPacket(clientID, client.Name).Serialize());
                    break;
                case ClientPacketType.SendMessage:
                    var sendMessagePacket = new SendMessagePacket(stream);

                    // Ignore packet if client handshake hasn't finished.
                    if (client.Name is null)
                    {
                        break;
                    }

                    ReceiveMessagePacket responsePacket = new ReceiveMessagePacket(
                        clientID,
                        sendMessagePacket.TargetUserID,
                        GetUnixTime(),
                        sendMessagePacket.Message);

                    // Debug display chatmessage.
                    if (!(Logger is null))
                    {
                        DebugDisplayChatMessage(clientID, sendMessagePacket.TargetUserID, sendMessagePacket.Message);
                    }

                    // Store chatmessage if public.
                    if (sendMessagePacket.TargetUserID == 0)
                    {
                        recallableMessages.Enqueue((client.Name, responsePacket));

                        // Shorten recallable message queue to configuration
                        while (recallableMessages.Count > config.MaxStoredMessages)
                        {
                            recallableMessages.Dequeue();
                        }

                        SendPacketAll(responsePacket.Serialize());
                    }
                    else
                    {
                        SendPacket(clientID, client, responsePacket.Serialize());

                        if (!clients.TryGetValue(sendMessagePacket.TargetUserID, out ClientInfo targetClient))
                        {
                            Logger?.Warning("Target user ID not found: " + sendMessagePacket.TargetUserID);
                            break;
                        }

                        SendPacket(sendMessagePacket.TargetUserID, targetClient, responsePacket.Serialize());
                    }

                    break;
                case ClientPacketType.Disconnect:
                    Logger?.Info($"Client: {client.Name} has disconnected");

                    ServerLogAll($"{client.Name} smuttede igen!");
                    DisconnectClient(clientID);
                    break;
                default:
                    break;
            }
        }

        private void ServerLogAll(string serverMessage)
        {
            SendPacketAll(new LogMessagePacket(GetUnixTime(), serverMessage).Serialize());
        }

        private void DebugDisplayChatMessage(byte authorID, byte targetID, string chatMsg)
        {
            const int minimumDisplayedMessage = 15;

            StringBuilder outputMsg = new StringBuilder();
            outputMsg.Append($"User {authorID} messaged {targetID}: ");

            int messageLength = Math.Min(
                chatMsg.Length,
                Math.Max(
                    Console.BufferWidth - outputMsg.Length - 7,
                    minimumDisplayedMessage));

            outputMsg.Append(chatMsg.Substring(0, messageLength));

            if (messageLength < chatMsg.Length)
            {
                outputMsg.Append("[...]");
            }

            Logger?.Info(outputMsg.ToString());
        }

        private void IntroduceClientToContext(byte clientID, ClientInfo client)
        {
            // Recall messages
            var messagesToRecall = new Queue<(string Name, ReceiveMessagePacket)>(recallableMessages);

            // Tuples .GetHashcode combines its element's hashcodes.
            HashSet<(byte, string)> updatedUserinfo = new HashSet<(byte, string)>();

            while (messagesToRecall.Count > 0)
            {
                (string authorname, ReceiveMessagePacket packet) = messagesToRecall.Dequeue();

                if (updatedUserinfo.Add((packet.UserID, authorname)))
                {
                    // Send UpdateUserInfo
                    SendPacket(clientID, client, new SendUserInfoPacket(packet.UserID, authorname).Serialize());
                }

                SendPacket(clientID, client, packet.Serialize());
            }

            // Disconnect old people
            foreach ((byte, string) item in updatedUserinfo)
            {
                // Only disconnect people not present anymore.
                if (clients.ContainsKey(item.Item1))
                {
                    continue;
                }

                SendPacket(clientID, client, new UserLeftPacket(item.Item1).Serialize());
            }

            // Send missing current user information
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

                SendPacket(clientID, client, new SendUserInfoPacket(otherClientID, otherClientName).Serialize());
            }
        }

        private byte? GetNextUserID()
        {
            byte startID = recentID;
            if (recentID == byte.MaxValue)
            {
                // Søg forfra.
                startID = 1;
            }

            for (byte i = startID; i < byte.MaxValue; i++)
            {
                if (!clients.ContainsKey(i) && !newClients.ContainsKey(i))
                {
                    recentID++;
                    return i;
                }
            }

            return null;
        }

        private long GetUnixTime() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        private void DisconnectClient(byte userID)
        {
            Logger?.Info($"Disconnected ID: {userID}");

            if (!clients.TryGetValue(userID, out ClientInfo clientInfo))
            {
                Logger?.Warning($"Trying to disconnect removed client: {userID}");
                return;
            }

            if (!(clientInfo.Name is null))
            {
                usedNames.Remove(clientInfo.Name);
            }

            clientInfo.TcpClient?.Close();

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
            catch (Exception ex) when (ex is IOException || ex is InvalidOperationException)
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
