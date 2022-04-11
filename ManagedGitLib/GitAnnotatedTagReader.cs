#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Buffers;
using ManagedGitLib.Parsers;

namespace ManagedGitLib
{
    /// <summary>
    /// Reads an annotated <see cref="GitTag"/> object.
    /// </summary>
    public static class GitAnnotatedTagReader
    {
        /// <summary>
        /// Reads an annotated <see cref="GitCommit"/> object from a <see cref="Stream"/>.
        /// </summary>
        /// <param name="stream">
        /// A <see cref="Stream"/> which contains the annotated <see cref="GitTag"/> in its text representation.
        /// </param>
        /// <param name="sha">
        /// The <see cref="GitObjectId"/> of the annotated tag.
        /// </param>
        /// <returns>
        /// The <see cref="GitTag"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static GitTag Read(Stream stream, GitObjectId sha)
        {
            if (stream is null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            byte[] buffer = ArrayPool<byte>.Shared.Rent((int)stream.Length);

            try
            {
                Span<byte> span = buffer.AsSpan(0, (int)stream.Length);
                stream.ReadAll(span);

                return Read(span, sha);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        /// <summary>
        /// Reads an annotated <see cref="GitTag"/> object from a <see cref="ReadOnlySpan{T}"/>.
        /// </summary>
        /// <param name="tag">
        /// A read-only buffer which contains the annotated <see cref="GitTag"/> in its text representation.
        /// </param>
        /// <param name="sha">
        /// The <see cref="GitObjectId"/> of the annotated tag.
        /// </param>
        /// <returns>
        /// The annotated <see cref="GitTag"/>.
        /// </returns>
        public static GitTag Read(ReadOnlySpan<byte> tag, GitObjectId sha)
        {
            string tagWorkload = GitRepository.GetString(tag);

            var parseResult = Parsers.Parsers.ParseTag(tagWorkload);

            var targetObject = GitObjectId.Parse(parseResult.@object);

            string type = parseResult.typeOf;

            string tagName = parseResult.name;

            GitSignature tagger = new GitSignature
            {
                Name = parseResult.tagger.name,
                Email = parseResult.tagger.email,
                Date = DateTimeOffset.FromUnixTimeSeconds(parseResult.tagger.date)
            };

            List<GitAdditionalHeader>? additionalHeaders = null;

            if (parseResult.additionalHeaders.Length != 0)
            {
                additionalHeaders = new List<GitAdditionalHeader>();

                foreach (var h in parseResult.additionalHeaders)
                {
                    additionalHeaders.Add(new GitAdditionalHeader
                    {
                        Key = h.name,
                        Value = h.value
                    });
                }
            }

            string message = parseResult.message;

            return new GitTag
            {
                Sha = sha,
                Target = targetObject,
                IsAnnotated = true,
                TargetType = type,
                Name = tagName,
                Tagger = tagger,
                AdditionalHeaders = additionalHeaders,
                Message = message
            };
        }
    }
}
