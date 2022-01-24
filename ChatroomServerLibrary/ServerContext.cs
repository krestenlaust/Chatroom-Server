using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using ChatroomServer.ClientPackets;
using ChatroomServer.EventArguments;
using ChatroomServer.ServerPackets;

#nullable enable
namespace ChatroomServer
{
    /// <summary>
    /// A chatroom server instance.
    /// </summary>
    public class ServerContext : IDisposable
    {
        public const string DefaultName = "Bonfire bruger";

        private readonly Dictionary<byte, Client> clients = new Dictionary<byte, Client>();
        private readonly IUserIDDispenser<byte> userIDDispenser = new UserIDDispenser();
        private readonly INameRegistrar nameRegistrar;
        private readonly INameValidator nameValidator;
        private readonly TcpListener tcpListener;
        private readonly List<IPacketReceived> packetReceivedPlugins = new List<IPacketReceived>();
        private readonly List<IHandshakeFinished> handshakeFinishedPlugins = new List<IHandshakeFinished>();

        /// <summary>
        /// Initializes a new instance of the <see cref="ServerContext"/> class.
        /// </summary>
        /// <param name="port">The port on which to start the server.</param>
        /// <param name="config">The configuration of the server.</param>
        public ServerContext(int port, ServerConfig config)
        {
            tcpListener = new TcpListener(System.Net.IPAddress.Any, port);
            this.Config = config;

            var nameManager = new NameManager(config.MinNameLength, config.MaxNameLength);
            nameRegistrar = nameManager;
            nameValidator = nameManager;

            RegisterServerFeature(new Features.MessageOfTheDay());
            RegisterServerFeature(new Features.RecallMessages());
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ServerContext"/> class.
        /// </summary>
        /// <param name="port">The port on which to start the server.</param>
        /// <param name="config">The configuration of the server.</param>
        /// <param name="logger">The logger with which to log information.</param>
        public ServerContext(int port, ServerConfig config, Logger logger)
        {
            tcpListener = new TcpListener(System.Net.IPAddress.Any, port);
            this.Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.Config = config;

            var nameManager = new NameManager(config.MinNameLength, config.MaxNameLength);
            nameRegistrar = nameManager;
            nameValidator = nameManager;

            RegisterServerFeature(new Features.MessageOfTheDay());
            RegisterServerFeature(new Features.RecallMessages());
        }

        public Logger? Logger { get; set; }

        public ServerConfig Config { get; }

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
                    if (timeDifference <= Config.HandshakeTimeout)
                    {
                        continue;
                    }

                    Logger?.Warning($"handshake timed out", ("ID", client.Key), ("Endpoint", client.Value.TcpClient.Client.RemoteEndPoint));
                    DisconnectClient(client.Key);

                    continue;
                }

                if (timeDifference <= Config.MaxTimeSinceLastActive)
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

                    HandlePacket(stream, packetType, client.Value);
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

        public void RegisterServerFeature(object pluginInstance)
        {
            if (pluginInstance is IHandshakeFinished handshakeFinished)
            {
                handshakeFinishedPlugins.Add(handshakeFinished);
            }

            if (pluginInstance is IPacketReceived packetReceived)
            {
                packetReceivedPlugins.Add(packetReceived);
            }
        }

        /// <summary>
        /// Called after default packet handling.
        /// </summary>
        /// <param name="e">Event argument.</param>
        protected virtual void OnPacketReceived(PacketReceivedEventArgs e)
        {
            foreach (var item in packetReceivedPlugins)
            {
                item?.OnPacketReceived(this, e);
            }
        }

        /// <summary>
        /// Called before sending context.
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnHandshakeFinished(HandshakeFinishedEventArgs e)
        {
            foreach (var item in handshakeFinishedPlugins)
            {
                item?.OnHandshakeFinished(this, e);
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

                /* // Update: Maybe implement so clients keep track of this.
                // Client name has already been sent.
                if (updatedUserinfo.Contains((otherClientID, otherClientName)))
                {
                    continue;
                }*/

                e.Client.SendPacket(new SendUserInfoPacket(otherClientID, otherClientName));
            }
        }

        /// <summary>
        /// Handles incoming packet.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="packetType"></param>
        /// <param name="client"></param>
        private void HandlePacket(NetworkStream stream, ClientPacketType packetType, Client client)
        {
            ClientPacket? packetReceived = null;
            ServerPacket? responsePacket = null;

            // Parse and handle the packets ChangeNamePacket and SendMessagePacket,
            // by responding to all other clients with a message or a userinfo update
            switch (packetType)
            {
                case ClientPacketType.ChangeName:
                    var changeNamePacket = new ChangeNamePacket(stream);
                    packetReceived = changeNamePacket;

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
                        Logger?.Info("has connected", ("ID", client.ID), ("Name", client.Name));
                        ServerLogAll($"{client.Name} forbandt!");

                        OnHandshakeFinished(new HandshakeFinishedEventArgs(client));
                    }
                    else
                    {
                        Logger?.Info($"changed their name to {client.Name}", ("ID", client.ID), ("Name", oldName));
                        ServerLogAll($"{oldName} skiftede navn til {client.Name}");
                    }

                    SendUserInfoPacket userInfoPacket = new SendUserInfoPacket(client.ID, client.Name);
                    responsePacket = userInfoPacket;

                    SendPacketAll(userInfoPacket);
                    break;
                case ClientPacketType.SendMessage:
                    var sendMessagePacket = new SendMessagePacket(stream);
                    packetReceived = sendMessagePacket;

                    // Ignore packet if client handshake hasn't finished.
                    if (client.Name is null)
                    {
                        break;
                    }

                    ReceiveMessagePacket receiveMessagePacket = new ReceiveMessagePacket(
                        client.ID,
                        sendMessagePacket.TargetUserID,
                        sendMessagePacket.Message);
                    responsePacket = receiveMessagePacket;

                    // DebugDisplayChatMessage(clientID, sendMessagePacket.TargetUserID, sendMessagePacket.Message);
                    Logger?.Info($"messaged ID {sendMessagePacket.TargetUserID}: \"{sendMessagePacket.Message}\"", ("ID", client.ID), ("Name", client.Name));

                    // Store chatmessage if public.
                    if (sendMessagePacket.TargetUserID == 0)
                    {
                        SendPacketAll(receiveMessagePacket);
                    }
                    else
                    {
                        client.SendPacket(receiveMessagePacket);

                        if (!clients.TryGetValue(sendMessagePacket.TargetUserID, out Client targetClient))
                        {
                            Logger?.Warning("Target user ID not found: " + sendMessagePacket.TargetUserID);
                            break;
                        }

                        clients[sendMessagePacket.TargetUserID].SendPacket(responsePacket);
                    }

                    break;
                case ClientPacketType.Disconnect:
                    packetReceived = new DisconnectPacket();
                    Logger?.Info($"signaled disconnection", ("ID", client.ID), ("Name", client.Name));

                    ServerLogAll($"{client.Name} forsvandt!");
                    DisconnectClient(client.ID);
                    break;
                default:
                    break;
            }

            if (packetReceived is null)
            {
                Logger?.Warning("Packet not recognized");
                return;
            }

            // Call event subscribers
            OnPacketReceived(new PacketReceivedEventArgs(client, packetReceived, packetType, responsePacket));
        }

        private void ServerLogAll(string serverMessage)
        {
            SendPacketAll(new LogMessagePacket(serverMessage));
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

        private void SendPacketAll(ServerPacket packet, byte exceptUser = 0)
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
