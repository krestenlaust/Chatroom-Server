﻿using System;
using System.Collections.Generic;
using System.Text;

namespace ChatroomServer
{
    /// <summary>
    /// Configuration settings for a server.
    /// </summary>
    public readonly struct ServerConfig
    {
        /// <summary>
        /// The timeout duration, before a client is disconnected, specified in milliseconds.
        /// </summary>
        public readonly int MaxTimeSinceLastActive;

        /// <summary>
        /// The timeout duration, before a client is disconnected during a handshake if not responding.
        /// </summary>
        public readonly int HandshakeTimeout;

        /// <summary>
        /// The highest amount of messages stored to later be recalled when a client connects.
        /// </summary>
        public readonly int MaxStoredMessages;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServerConfig"/> struct.
        /// </summary>
        /// <param name="maxTimeSinceLastActive">The timeout duration, before a client is disconnected, specified in milliseconds.</param>
        /// <param name="maxStoredMessages">The highest amount of messages stored to later be recalled when a client connects.</param>
        public ServerConfig(int maxTimeSinceLastActive, int handshakeTimeout, int maxStoredMessages)
        {
            MaxTimeSinceLastActive = maxTimeSinceLastActive;
            HandshakeTimeout = handshakeTimeout;
            MaxStoredMessages = maxStoredMessages;
        }

        /// <summary>
        /// Gets the default server configuration:
        /// <c>MaxTimeSinceLastActive</c> = 100 ms,
        /// <c>HandshakeTimeout</c> = 1000 ms,
        /// <c>MaxStoredMessages</c> = 10.
        /// </summary>
        public static ServerConfig Default => new ServerConfig(
            maxTimeSinceLastActive: 100,
            handshakeTimeout: 1000,
            maxStoredMessages: 10
            );
    }
}
