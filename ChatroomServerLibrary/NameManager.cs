using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

#nullable enable
namespace ChatroomServer
{
    /// <summary>
    /// Keeps track of names, and of validating them.
    /// </summary>
    public class NameManager : INameRegistrar, INameValidator
    {
        private readonly HashSet<string> usedNames = new HashSet<string>();
        private readonly int minLength;
        private readonly int maxLength;

        /// <summary>
        /// Initializes a new instance of the <see cref="NameManager"/> class.
        /// </summary>
        /// <param name="minNameLength">Shortest allowed name length (inclusive).</param>
        /// <param name="maxNameLength">Longest allowed name length (inclusive).</param>
        public NameManager(int minNameLength, int maxNameLength)
        {
            minLength = minNameLength;
            maxLength = maxNameLength;
        }

        /// <inheritdoc/>
        public bool IsNameValid(string name)
        {
            if (name is null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            return FixName(name) == name;
        }

        /// <inheritdoc/>
        public string? FixName(string name)
        {
            if (name is null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            string fixedName = name;

            // Replace single whitespace.
            fixedName = fixedName.Replace(' ', '_');

            // Clean of non-printable characters (including zero-width space)
            fixedName = Regex.Replace(fixedName, "[\x00\x08\x0B\x0C\x0E-\x1F\u200B]", string.Empty);

            // Truncate if longer than possible.
            fixedName = fixedName.Substring(0, Math.Min(maxLength, fixedName.Length));

            // Enumerate name if multiple users have the same name.
            string enumeratedName = fixedName;

            int nextIndex = 2;
            while (!IsNameAvailable(enumeratedName))
            {
                enumeratedName = $"{fixedName} ({nextIndex++})";
            }

            // If name is too short, or has been extended beyond limit, then return null.
            if (enumeratedName.Length < minLength || enumeratedName.Length > maxLength)
            {
                return null;
            }

            return fixedName;
        }

        /// <inheritdoc/>
        public bool RegisterName(string name)
        {
            if (name is null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            return usedNames.Add(name);
        }

        /// <inheritdoc/>
        public void DeregisterName(string name)
        {
            if (name is null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            usedNames.Remove(name);
        }

        /// <inheritdoc/>
        public bool IsNameAvailable(string name)
        {
            if (name is null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            return !usedNames.Contains(name);
        }
    }
}
