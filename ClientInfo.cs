using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace ChatrumServer
{
    public class ClientInfo
    {
        public string Name;
        public TcpClient TcpClient;
        public long TimeSinceActive;

        public ClientInfo(TcpClient tcpClient, long timeSinceActive)
        {

        }
    }
}
