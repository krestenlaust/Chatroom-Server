using System;
using System.Collections.Generic;
using ChatroomServer.ClientPackets;
using ChatroomServer.EventArguments;
using ChatroomServer.ServerPackets;

namespace ChatroomServer.Features
{
    /// <summary>
    /// Plugin class for recalling messages when a user joins a server.
    /// </summary>
    public class RecallMessages : IHandshakeFinished, IPacketReceived
    {
        private readonly Queue<(string Name, ReceiveMessagePacket StoredMessage)> recallableMessages = new Queue<(string Name, ReceiveMessagePacket StoredMessage)>();

        /// <inheritdoc/>
        public void OnHandshakeFinished(ServerContext serverContext, HandshakeFinishedEventArgs e)
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
                    e.Client.SendPacket(new SendUserInfoPacket(receiveMessagePacket.UserID, authorname));
                }

                e.Client.SendPacket(receiveMessagePacket);
            }

            // Disconnect previous senders.
            foreach ((byte, string) item in updatedUserinfo)
            {
                /* // Update: disconnects everyone, to then let them be introduced later.
                // Only disconnect people not present anymore.
                if (clients.ContainsKey(item.Item1))
                {
                    continue;
                }*/

                e.Client.SendPacket(new UserLeftPacket(item.Item1));
            }
        }

        /// <inheritdoc/>
        public void OnPacketReceived(ServerContext serverContext, PacketReceivedEventArgs e)
        {
            if (e.PacketType != ClientPacketType.SendMessage)
            {
                return;
            }

            SendMessagePacket sendMessagePacket = (SendMessagePacket)e.Packet;
            ReceiveMessagePacket responseMessagePacket = (ReceiveMessagePacket)e.ResponsePacket;

            // If chatmessage is private, don't store it.
            if (sendMessagePacket.TargetUserID != 0)
            {
                return;
            }

            recallableMessages.Enqueue((e.Client.Name, responseMessagePacket));

            // Shorten recallable message queue to configuration
            while (recallableMessages.Count > serverContext.Config.MaxStoredMessages)
            {
                recallableMessages.Dequeue();
            }
        }
    }
}
