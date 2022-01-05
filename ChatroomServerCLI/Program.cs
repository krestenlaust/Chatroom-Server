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

        private static void Main(string[] args)
        {
            const short serverPort = 25565;

            Logger serverLogger;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                serverLogger = new ANSILogger();
            }
            else
            {
                serverLogger = new ConsoleLogger();
            }

            Console.WriteLine($"Logging with verbosity level: {Enum.GetName(typeof(LogType), serverLogger.LogLevel)}");

            ServerConfig selectedConfig = ServerConfig.Default;
#if DEBUG
            selectedConfig = new ServerConfig(99999, 10);
#endif

            using Server server = new Server(serverPort, selectedConfig, serverLogger);
            server.Start();
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
