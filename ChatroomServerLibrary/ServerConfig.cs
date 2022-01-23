#nullable enable
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
        /// The duration before a handshake is timed out.
        /// </summary>
        public readonly int HandshakeTimeout;

        /// <summary>
        /// The highest amount of messages stored to later be recalled when a client connects.
        /// </summary>
        public readonly int MaxStoredMessages;

        /// <summary>
        /// The message to sent new users. If null, no message is sent.
        /// </summary>
        public readonly string? MessageOfTheDay;

        /// <summary>
        /// Shortest allowed name length (inclusive).
        /// </summary>
        public readonly int MinNameLength;

        /// <summary>
        /// Longest allowed name length (inclusive).
        /// </summary>
        public readonly int MaxNameLength;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServerConfig"/> struct.
        /// </summary>
        /// <param name="maxTimeSinceLastActive">The timeout duration, before a client is disconnected, specified in milliseconds.</param>
        /// <param name="handshakeTimeout">The duration before a handshake is timed out.</param>
        /// <param name="maxStoredMessages">The highest amount of messages stored to later be recalled when a client connects.</param>
        /// <param name="messageOfTheDay">The message to sent new users. If null, no message is sent.</param>
        /// <param name="minNameLength">Shortest allowed name length (inclusive).</param>
        /// <param name="maxNameLength">Longest allowed name length (inclusive).</param>
        public ServerConfig(int maxTimeSinceLastActive = 100, int handshakeTimeout = 1000, int maxStoredMessages = 10, string? messageOfTheDay = null, int minNameLength = 1, int maxNameLength = 20)
        {
            MaxTimeSinceLastActive = maxTimeSinceLastActive;
            HandshakeTimeout = handshakeTimeout;
            MaxStoredMessages = maxStoredMessages;
            MessageOfTheDay = messageOfTheDay;
            MinNameLength = minNameLength;
            MaxNameLength = maxNameLength;
        }
    }
}
