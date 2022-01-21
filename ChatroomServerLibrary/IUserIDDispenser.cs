using System;

namespace ChatroomServer
{
    /// <summary>
    /// Describes the implementation for managing user ID's.
    /// </summary>
    /// <typeparam name="T">The </typeparam>
    public interface IUserIDDispenser<T>
        where T : struct, IComparable, IComparable<T>, IConvertible, IEquatable<T>
    {
        /// <summary>
        /// Gets the next available userID.
        /// </summary>
        /// <returns>Returns null if no ID's are available.</returns>
        public T? GetNext();

        /// <summary>
        /// Makes the ID available again.
        /// </summary>
        /// <param name="id">The ID to be released.</param>
        public void ReleaseID(T id);
    }
}
