using System;
using System.Collections.Generic;
using System.Text;

namespace ChatroomServer
{
    /// <summary>
    /// The most general class for packets.
    /// </summary>
    /// <typeparam name="T">The enumuration of types of packets and their ID.</typeparam>
    public abstract class Packet<T>
        where T : Enum
    {
        /// <summary>
        /// Gets or sets the type of packet.
        /// </summary>
        public T PacketType { get; protected set; }
    }
}
