namespace ChatroomServer
{
    /// <summary>
    /// The name registrar keeps track of which names are in use and occupied.
    /// </summary>
    public interface INameRegistrar
    {
        /// <summary>
        /// Registers the name.
        /// </summary>
        /// <param name="name">The name to register.</param>
        /// <returns>True if the name has been registered successfully.</returns>
        public bool RegisterName(string name);

        /// <summary>
        /// Releases the name to allow future registration.
        /// </summary>
        /// <param name="name">The name to unregister.</param>
        public void DeregisterName(string name);

        /// <summary>
        /// Returns whether the name is available.
        /// </summary>
        /// <param name="name">The name to check for availability.</param>
        /// <returns>True if the name is already registered.</returns>
        public bool IsNameAvailable(string name);
    }
}
