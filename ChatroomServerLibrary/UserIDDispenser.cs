using System;
using System.Collections.Generic;
using System.Text;

namespace ChatroomServer
{
    /// <summary>
    /// Helper class for delegating user ID's of type <c>byte</c>.
    /// </summary>
    public class UserIDDispenser : IUserIDDispenser<byte>
    {
        private readonly Stack<byte> poolOfID = new Stack<byte>(byte.MaxValue);

        /// <summary>
        /// Initializes a new instance of the <see cref="UserIDDispenser"/> class.
        /// </summary>
        public UserIDDispenser()
        {
            for (byte i = byte.MaxValue; i > 0; i--)
            {
                poolOfID.Push(i);
            }
        }

        /// <inheritdoc/>
        public byte? GetNext()
        {
            if (poolOfID.Count == 0)
            {
                return null;
            }

            return poolOfID.Pop();
        }

        /// <inheritdoc/>
        public void ReleaseID(byte id)
        {
            if (poolOfID.Contains(id))
            {
                return;
            }

            poolOfID.Push(id);
        }
    }
}
