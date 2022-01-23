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
        private const string DefaultName = "Bonfire bruger";
        public Logger? Logger;
        private readonly Dictionary<byte, ClientInfo> clients = new Dictionary<byte, ClientInfo>();
        private readonly Queue<(string Name, ReceiveMessagePacket StoredMessage)> recallableMessages = new Queue<(string Name, ReceiveMessagePacket StoredMessage)>();
        private readonly IUserIDDispenser<byte> userIDDispenser = new UserIDDispenser();
        private readonly INameRegistrar nameRegistrar;
        private readonly INameValidator nameValidator;
        private readonly TcpListener tcpListener;
        private readonly ServerConfig config;

        /// <summary>
        /// Initializes a new instance of the <see cref="Server"/> class.
        /// </summary>
        /// <param name="port">The port on which to start the server.</param>
        /// <param name="config">The configuration of the server.</param>
        public Server(int port, ServerConfig config)
        {
            tcpListener = new TcpListener(System.Net.IPAddress.Any, port);
            this.config = config;

            var nameManager = new NameManager(config.MinNameLength, config.MaxNameLength);
            nameRegistrar = nameManager;
            nameValidator = nameManager;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Server"/> class.
        /// </summary>
        /// <param name="port">The port on which to start the server.</param>
        /// <param name="config">The configuration of the server.</param>
        /// <param name="logger">The logger with which to log information.</param>
        public Server(int port, ServerConfig config, Logger logger)
        {
            tcpListener = new TcpListener(System.Net.IPAddress.Any, port);
            this.Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.config = config;

            var nameManager = new NameManager(config.MinNameLength, config.MaxNameLength);
            nameRegistrar = nameManager;
            nameValidator = nameManager;
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

                byte? newUserID = userIDDispenser.GetNext();

                // If lobby is full, turn away client.
                if (newUserID is null)
                {
                    client.Close();
                    Logger?.Warning("User ID dispenser empty: Lobby full");
                    return;
                }

                // Assign client their ID
                stream.Write(new SendUserIDPacket(newUserID.Value).Serialize());

                ClientInfo clientInfo = new ClientInfo(client);
                clients.Add(newUserID.Value, clientInfo);

                Logger?.Info("has connected", ("ID", newUserID.Value), ("Endpoint", client.Client.RemoteEndPoint));
            }

            // Pings clients
            foreach (var client in clients)
            {
                // Ping client if more time than whats good, has passed.
                long timeDifference = (long)(DateTime.UtcNow - client.Value.LastActiveUTCTime).TotalMilliseconds;

                // Client hasn't handshaked yet.
                if (client.Value.Name is null)
                {
                    if (timeDifference <= config.HandshakeTimeout)
                    {
                        continue;
                    }

                    Logger?.Warning($"handshake timed out", ("ID", client.Key), ("Endpoint", client.Value.TcpClient.Client.RemoteEndPoint));
                    DisconnectClient(client.Key);

                    continue;
                }

                if (timeDifference <= config.MaxTimeSinceLastActive)
                {
                    continue;
                }

                SendPacket(client.Key, new PingPacket());
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
                    client.Value.UpdateLastActiveTime();

                    byte packetID = (byte)stream.ReadByte();

                    if (!Enum.IsDefined(typeof(ClientPacketType), packetID))
                    {
                        // Unknown packet: Everything is fine!
                        Logger?.Warning($"Received unknown packet", ("ID", client.Key), ("Endpoint", client.Value.TcpClient.Client.RemoteEndPoint));
                        continue;
                    }

                    var packetType = (ClientPacketType)packetID;

                    OnPacketReceived(stream, packetType, client.Key, client.Value);
                }
                catch (InvalidOperationException ex)
                {
                    Logger?.Warning($"Client disconnect by invalid operation: {ex}", ("ID", client.Key));
                    DisconnectClient(client.Key);
                }
                catch (SocketException ex)
                {
                    Logger?.Warning($"Client disconnected by Socket exception: {ex}", ("ID", client.Key));
                    DisconnectClient(client.Key);
                }
                catch (Exception ex)
                {
                    Logger?.Error(ex.ToString());
                    throw;
                }
            }
        }

        private static long GetUnixTime() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        /// <summary>
        /// Handles incoming packet.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="packetType"></param>
        /// <param name="userID"></param>
        /// <param name="client"></param>
        private void OnPacketReceived(NetworkStream stream, ClientPacketType packetType, byte userID, ClientInfo client)
        {
            // Parse and handle the packets ChangeNamePacket and SendMessagePacket,
            // by responding to all other clients with a message or a userinfo update
            switch (packetType)
            {
                case ClientPacketType.ChangeName:
                    var changeNamePacket = new ChangeNamePacket(stream);

                    string? oldName = client.Name;

                    if (!(oldName is null))
                    {
                        nameRegistrar.DeregisterName(oldName);
                    }

                    string? fixedName = nameValidator.FixName(changeNamePacket.Name);
                    if (fixedName is null)
                    {
                        client.Name = nameValidator.FixName(DefaultName);
                    }
                    else
                    {
                        client.Name = fixedName;
                    }

                    try
                    {
                        nameRegistrar.RegisterName(client.Name);
                    }
                    catch (ArgumentNullException ex)
                    {
                        Logger?.Error(ex.ToString());
                        throw;
                    }

                    // First time connecting
                    if (oldName is null)
                    {
                        OnClientHandshakeFinished(userID);

                        Logger?.Info("has connected", ("ID", userID), ("Name", client.Name));
                        ServerLogAll($"{client.Name} forbandt!");
                    }
                    else
                    {
                        Logger?.Info($"changed their name to {client.Name}", ("ID", userID), ("Name", oldName));
                        ServerLogAll($"{oldName} skiftede navn til {client.Name}");
                    }

                    SendPacketAll(new SendUserInfoPacket(userID, client.Name));
                    break;
                case ClientPacketType.SendMessage:
                    var sendMessagePacket = new SendMessagePacket(stream);

                    // Ignore packet if client handshake hasn't finished.
                    if (client.Name is null)
                    {
                        break;
                    }

                    ReceiveMessagePacket responsePacket = new ReceiveMessagePacket(
                        userID,
                        sendMessagePacket.TargetUserID,
                        GetUnixTime(),
                        sendMessagePacket.Message);

                    // Debug display chatmessage.
                    if (!(Logger is null))
                    {
                        // DebugDisplayChatMessage(clientID, sendMessagePacket.TargetUserID, sendMessagePacket.Message);
                        Logger.Info($"messaged ID {sendMessagePacket.TargetUserID}: \"{sendMessagePacket.Message}\"", ("ID", userID), ("Name", client.Name));
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

                        SendPacketAll(responsePacket);
                    }
                    else
                    {
                        SendPacket(userID, responsePacket);

                        if (!clients.TryGetValue(sendMessagePacket.TargetUserID, out ClientInfo targetClient))
                        {
                            Logger?.Warning("Target user ID not found: " + sendMessagePacket.TargetUserID);
                            break;
                        }

                        SendPacket(sendMessagePacket.TargetUserID, responsePacket);
                    }

                    break;
                case ClientPacketType.Disconnect:
                    Logger?.Info($"signaled disconnection", ("ID", userID), ("Name", client.Name));

                    ServerLogAll($"{client.Name} forsvandt!");
                    DisconnectClient(userID);
                    break;
                default:
                    break;
            }
        }

        private void ServerLog(byte userID, string serverMessage)
        {
            SendPacket(userID, new LogMessagePacket(GetUnixTime(), serverMessage));
        }

        private void ServerLogAll(string serverMessage)
        {
            SendPacketAll(new LogMessagePacket(GetUnixTime(), serverMessage));
        }

        /*
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
        }*/

        private void OnClientHandshakeFinished(byte clientID)
        {
            // Recall messages
            var messagesToRecall = new Queue<(string Name, ReceiveMessagePacket)>(recallableMessages);

            // Collection of unique user information.
            HashSet<(byte, string)> updatedUserinfo = new HashSet<(byte, string)>();

            while (messagesToRecall.Count > 0)
            {
                (string authorname, ReceiveMessagePacket receiveMessagePacket) = messagesToRecall.Dequeue();

                if (updatedUserinfo.Add((receiveMessagePacket.UserID, authorname)))
                {
                    // Send UpdateUserInfo
                    SendPacket(clientID, new SendUserInfoPacket(receiveMessagePacket.UserID, authorname));
                }

                SendPacket(clientID, receiveMessagePacket);
            }

            // Disconnect absent people
            foreach ((byte, string) item in updatedUserinfo)
            {
                // Only disconnect people not present anymore.
                if (clients.ContainsKey(item.Item1))
                {
                    continue;
                }

                SendPacket(clientID, new UserLeftPacket(item.Item1));
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

                SendPacket(clientID, new SendUserInfoPacket(otherClientID, otherClientName));
            }

            // Message of the day
            if (!(config.MessageOfTheDay is null))
            {
                ServerLog(clientID, config.MessageOfTheDay);
            }
        }

        private void DisconnectClient(byte userID)
        {
            Logger?.Info($"has disconnected", ("ID", userID), ("Name", clients[userID].Name));

            if (!clients.TryGetValue(userID, out ClientInfo clientInfo))
            {
                Logger?.Warning($"Trying to disconnect removed client: {userID}");
                return;
            }

            if (!(clientInfo.Name is null))
            {
                nameRegistrar.DeregisterName(clientInfo.Name);
            }

            clientInfo.TcpClient?.Close();

            // Remove client.
            clients.Remove(userID);
            userIDDispenser.ReleaseID(userID);

            // Send UserLeftPacket to all clients iteratively.
            SendPacketAll(new UserLeftPacket(userID), userID);
        }

        private void SendPacket<T>(byte userID, T packet)
            where T : ServerPacket
        {
            clients[userID].UpdateLastActiveTime();
            byte[] serializedPacket = packet.Serialize();

            try
            {
                NetworkStream stream = clients[userID].TcpClient.GetStream();
                stream.Write(serializedPacket, 0, serializedPacket.Length);
            }
            catch (Exception ex) when (ex is IOException || ex is InvalidOperationException)
            {
                // Disconnect client because it isn't connected.
                DisconnectClient(userID);
            }
        }

        private void SendPacketAll<T>(T packet, byte exceptUser = 0)
            where T : ServerPacket
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

                SendPacket(client.Key, packet);
            }
        }
    }
}
