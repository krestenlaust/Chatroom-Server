using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace ChatroomServer
{
    public class ClientInfo
    {
        public string Name;
        public TcpClient TcpClient;
        public long LastActiveTime;

        public ClientInfo(TcpClient tcpClient, long lastActiveTime)
        {
            TcpClient = tcpClient;
            LastActiveTime = lastActiveTime;
        }
    }
}
