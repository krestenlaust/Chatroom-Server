using System;

namespace ChatroomServer.EventArguments
{
    public class HandshakeFinishedEventArgs : EventArgs
    {
        public HandshakeFinishedEventArgs(Client client)
        {
            Client = client;
        }

        public Client Client { get; }
    }
}
