using System;
using System.Diagnostics;
using System.Threading;
using ChatroomServer;

namespace ChatroomServer
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            Server server = new Server(25565);
            server.Start();
            Console.WriteLine("Listing on 0.0.0.0:25565");

            Stopwatch sw = new Stopwatch();
            sw.Start();
            while (true)
            {
                sw.Restart();
                server.Update();

                while (sw.ElapsedMilliseconds < 1000)
                    Thread.Sleep(0);
            }
        }
    }
}
