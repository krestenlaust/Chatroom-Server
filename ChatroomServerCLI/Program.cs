using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using ChatroomServer;
using ChatroomServer.Packets;

namespace ChatroomServerCLI
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            const short serverPort = 25565;
            Server server = new Server(serverPort);
            server.Start();
            Console.WriteLine($"Listening on {LocalIP}:{serverPort}");

            Stopwatch sw = new Stopwatch();
            sw.Start();
            while (true)
            {
                sw.Restart();
                server.Update();

                while (sw.ElapsedMilliseconds < 34)
                {
                    Thread.Sleep(0);
                }
            }
        }

        private static string LocalIP
        {
            get
            {
                IPAddress[] addresses = Dns.GetHostAddresses(Dns.GetHostName());
                foreach (IPAddress address in addresses)
                {
                    if (address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        return address.ToString();
                    }
                }

                return "0.0.0.0";
            }
        }
    }
}
