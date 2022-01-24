using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using ChatroomServer;
using ChatroomServer.Loggers;

namespace ChatroomServerCLI
{
    internal class Program
    {
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

        private const short DefaultPort = 25565;

        private static void Main(string[] args)
        {
            int serverPort;
            string environmentPort = Environment.GetEnvironmentVariable("PORT");
            if (environmentPort is null)
            {
                serverPort = DefaultPort;
            }
            else
            {
                serverPort = int.Parse(environmentPort);
            }

            Logger serverLogger;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                // serverLogger = new ANSILogger();
                serverLogger = new ConsoleLogger();
            }
            else
            {
                serverLogger = new ConsoleLogger();
            }

            Console.WriteLine($"Logging with verbosity level: {Enum.GetName(typeof(LogType), serverLogger.LogLevel)}");

            ServerConfig selectedConfig = new ServerConfig(
                maxTimeSinceLastActive: 1000,
                handshakeTimeout: 1000,
                maxStoredMessages: 10,
                messageOfTheDay: "Velkommen til denne Chatrum-protokol(™)-kompatible server (serverplads sponseret af https://LMHVPN.INFO)");

            using ServerContext server = Server.BindServer(serverPort, selectedConfig, serverLogger);
            Console.WriteLine($"Listening on {LocalIP}:{serverPort}");

            Stopwatch sw = new Stopwatch();
            sw.Start();

            try
            {
                while (true)
                {
                    sw.Restart();
                    server.Update();

                    while (sw.ElapsedMilliseconds < 1000 / 10)
                    {
                        Thread.Sleep(0);
                    }
                }
            }
            catch (Exception ex)
            {
                serverLogger.Error($"Exception occured: {ex}");
                throw;
            }
        }
    }
}
