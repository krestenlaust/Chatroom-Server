using System;
using System.Collections.Generic;
using System.Text;

namespace ChatroomServer
{
    public static class Server
    {
        public static ServerContext BindServer(int port, ServerConfig config, Logger logger)
        {
            ServerContext context = new ServerContext(port, config, logger);
            context.Start();

            return context;
        }
    }
}
