using System;

namespace ChatroomServer.ServerPackets
{
    public abstract class TimestampedPacket : ServerPacket
    {
        private static long GetUnixTime() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        /// <summary>
        /// Timestamp of the time the packet was created.
        /// </summary>
        public readonly long Timestamp;

        /// <summary>
        /// Initializes a new instance of the <see cref="TimestampedPacket"/> class with timestamp set as now.
        /// </summary>
        public TimestampedPacket() : base()
        {
            Timestamp = GetUnixTime();
        }
    }
}
