#nullable enable
namespace ChatroomServer
{
    /// <summary>
    /// The name validator specifies the limitations of naming.
    /// For example, the length of the name, and valid characters for names.
    /// </summary>
    public interface INameValidator
    {
        /// <summary>
        /// Specifies whether a name is valid.
        /// If true, <c>FixName</c> should return the name unchanged.
        /// </summary>
        /// <param name="name">The name to validate.</param>
        /// <returns>Whether validation was successful.</returns>
        public bool IsNameValid(string name);

        /// <summary>
        /// Returns a corrected version of the name specified.
        /// If name is already valid, returns unaltered string.
        /// </summary>
        /// <param name="name">The name to fix.</param>
        /// <returns>The validated version of the name. Returns null, if name is unfixable.</returns>
        public string? FixName(string name);
    }
}
