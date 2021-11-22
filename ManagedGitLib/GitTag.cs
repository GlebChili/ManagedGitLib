#nullable enable

using System;
using System.Collections.Generic;
using System.Text;

namespace ManagedGitLib
{
    /// <summary>
    /// Represents a Git tag, both lightweight and annotated.
    /// </summary>
    public struct GitTag
    {
        /// <summary>
        /// Gets or sets the name of the tag
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets <see cref="GitObjectId"/> ot the tag's target.
        /// </summary>
        public GitObjectId Target { get; set; }
        
        /// <summary>
        /// Gets or sets whether the tag is annotated
        /// </summary>
        public bool IsAnnotated { get; set; }

        /// <summary>
        /// Gets or sets a <see cref="GitObjectId"/> of the annotated tag.
        /// </summary>
        public GitObjectId Sha { get; set; }

        /// <summary>
        /// Gets or sets the target object type of the annotated tag.
        /// </summary>
        public string? TargetType { get; set; }

        /// <summary>
        /// Gets or sets the tagger of the annotated tag.
        /// </summary>
        public GitSignature? Tagger { get; set; }

        /// <summary>
        /// Gets otr sets the message of the annotated tag.
        /// </summary>
        public string? Message { get; set; }
    }
}
