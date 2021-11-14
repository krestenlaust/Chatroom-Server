using ChatroomServer;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace ChatroomServerCLI
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            Server server = new Server(25565);
            server.Start();
            Console.WriteLine($"Listening on {LocalIP}:25565");

            Stopwatch sw = new Stopwatch();
            sw.Start();
            while (true)
            {
                sw.Restart();
                server.Update();

                while (sw.ElapsedMilliseconds < 34)
                    Thread.Sleep(0);
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
