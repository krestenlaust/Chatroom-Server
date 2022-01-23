using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
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
        private readonly Dictionary<byte, Client> clients = new Dictionary<byte, Client>();
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
            // Remove disconnected clients.
            var disconnectedClients = clients.Where(c => c.Value.Disconnected).Select(c => c.Key);
            foreach (var clientID in disconnectedClients)
            {
                DisconnectClient(clientID);
            }

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

                Client clientInfo = new Client(client, newUserID.Value);
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

                client.Value.SendPacket(new PingPacket());
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

                    OnPacketReceived(stream, packetType, client.Value);
                }
                catch (InvalidOperationException ex)
                {
                    Logger?.Warning($"Client disconnected by invalid operation: {ex}", ("ID", client.Key));
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
        /// <param name="client"></param>
        private void OnPacketReceived(NetworkStream stream, ClientPacketType packetType, Client client)
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
                        OnClientHandshakeFinished(client);

                        Logger?.Info("has connected", ("ID", client.ID), ("Name", client.Name));
                        ServerLogAll($"{client.Name} forbandt!");
                    }
                    else
                    {
                        Logger?.Info($"changed their name to {client.Name}", ("ID", client.ID), ("Name", oldName));
                        ServerLogAll($"{oldName} skiftede navn til {client.Name}");
                    }

                    SendPacketAll(new SendUserInfoPacket(client.ID, client.Name));
                    break;
                case ClientPacketType.SendMessage:
                    var sendMessagePacket = new SendMessagePacket(stream);

                    // Ignore packet if client handshake hasn't finished.
                    if (client.Name is null)
                    {
                        break;
                    }

                    ReceiveMessagePacket responsePacket = new ReceiveMessagePacket(
                        client.ID,
                        sendMessagePacket.TargetUserID,
                        GetUnixTime(),
                        sendMessagePacket.Message);

                    // Debug display chatmessage.
                    if (!(Logger is null))
                    {
                        // DebugDisplayChatMessage(clientID, sendMessagePacket.TargetUserID, sendMessagePacket.Message);
                        Logger.Info($"messaged ID {sendMessagePacket.TargetUserID}: \"{sendMessagePacket.Message}\"", ("ID", client.ID), ("Name", client.Name));
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
                        client.SendPacket(responsePacket);

                        if (!clients.TryGetValue(sendMessagePacket.TargetUserID, out Client targetClient))
                        {
                            Logger?.Warning("Target user ID not found: " + sendMessagePacket.TargetUserID);
                            break;
                        }

                        clients[sendMessagePacket.TargetUserID].SendPacket(responsePacket);
                    }

                    break;
                case ClientPacketType.Disconnect:
                    Logger?.Info($"signaled disconnection", ("ID", client.ID), ("Name", client.Name));

                    ServerLogAll($"{client.Name} forsvandt!");
                    DisconnectClient(client.ID);
                    break;
                default:
                    break;
            }
        }

        private void ServerLog(Client user, string serverMessage)
        {
            user.SendPacket(new LogMessagePacket(GetUnixTime(), serverMessage));
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

        private void OnClientHandshakeFinished(Client client)
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
                    client.SendPacket(new SendUserInfoPacket(receiveMessagePacket.UserID, authorname));
                }

                client.SendPacket(receiveMessagePacket);
            }

            // Disconnect absent people
            foreach ((byte, string) item in updatedUserinfo)
            {
                // Only disconnect people not present anymore.
                if (clients.ContainsKey(item.Item1))
                {
                    continue;
                }

                client.SendPacket(new UserLeftPacket(item.Item1));
            }

            // Send missing current user information
            foreach (KeyValuePair<byte, Client> otherClientPair in clients)
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

                client.SendPacket(new SendUserInfoPacket(otherClientID, otherClientName));
            }

            // Message of the day
            if (!(config.MessageOfTheDay is null))
            {
                ServerLog(client, config.MessageOfTheDay);
            }
        }

        private void DisconnectClient(byte userID)
        {
            Logger?.Info($"has disconnected", ("ID", userID), ("Name", clients[userID].Name));

            if (!clients.TryGetValue(userID, out Client clientInfo))
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

                client.Value.SendPacket(packet);
            }
        }
    }
}
