using System;
namespace ManagedGitLib
{
    /// <summary>
    /// Represents additinal, optional header of the Git commit.
    /// </summary>
    public struct GitAdditionalHeader
    {
        /// <summary>
        /// Gets or sets the name of the header.
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// Gets or sets the value of the header.
        /// </summary>
        public string Value { get; set; }
    }
}

