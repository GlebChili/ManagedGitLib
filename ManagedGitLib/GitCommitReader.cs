#nullable enable

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using ManagedGitLib.Parsers;

namespace ManagedGitLib
{
    /// <summary>
    /// Reads a <see cref="GitCommit"/> object.
    /// </summary>
    public static class GitCommitReader
    {
        private static readonly byte[] TreeStart = GitRepository.Encoding.GetBytes("tree ");
        private static readonly byte[] ParentStart = GitRepository.Encoding.GetBytes("parent ");
        private static readonly byte[] AuthorStart = GitRepository.Encoding.GetBytes("author ");
        private static readonly byte[] CommitterStart = GitRepository.Encoding.GetBytes("committer ");
        private static readonly byte[] MessageStart = GitRepository.Encoding.GetBytes("\n");

        private const int TreeLineLength = 46;
        private const int ParentLineLength = 48;

        /// <summary>
        /// Reads a <see cref="GitCommit"/> object from a <see cref="Stream"/>.
        /// </summary>
        /// <param name="stream">
        /// A <see cref="Stream"/> which contains the <see cref="GitCommit"/> in its text representation.
        /// </param>
        /// <param name="sha">
        /// The <see cref="GitObjectId"/> of the commit.
        /// </param>
        /// <returns>
        /// The <see cref="GitCommit"/>.
        /// </returns>
        public static GitCommit Read(Stream stream, GitObjectId sha)
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
        /// Reads a <see cref="GitCommit"/> object from a <see cref="ReadOnlySpan{T}"/>.
        /// </summary>
        /// <param name="commit">
        /// A <see cref="ReadOnlySpan{T}"/> which contains the <see cref="GitCommit"/> in its text representation.
        /// </param>
        /// <param name="sha">
        /// The <see cref="GitObjectId"/> of the commit.
        /// </param>
        /// <returns>
        /// The <see cref="GitCommit"/>.
        /// </returns>
        public static GitCommit Read(ReadOnlySpan<byte> commit, GitObjectId sha)
        {
            string commitWorkload = GitRepository.GetString(commit);

            ParsedCommit parsedCommit = Parsers.Parsers.ParseCommitt(commitWorkload);

            GitObjectId tree = GitObjectId.Parse(parsedCommit.tree.hash);

            GitObjectId? firstParent = null;
            GitObjectId? secondParent = null;
            List<GitObjectId>? additionalParents = null;

            if (parsedCommit.parents.Length != 0)
            {
                firstParent = GitObjectId.Parse(parsedCommit.parents[0].hash);
            }

            if (parsedCommit.parents.Length > 1)
            {
                secondParent = GitObjectId.Parse(parsedCommit.parents[1].hash);
            }

            if (parsedCommit.parents.Length > 2)
            {
                var tmp = parsedCommit.parents.Skip(2);

                additionalParents = new List<GitObjectId>();

                foreach (var p in tmp)
                {
                    additionalParents.Add(GitObjectId.Parse(p.hash));
                }
            }

            GitSignature author = new GitSignature
            {
                Name = parsedCommit.author.name,
                Email = parsedCommit.author.email,
                Date = DateTimeOffset.FromUnixTimeSeconds(parsedCommit.author.date)
            };

            GitSignature committer = new GitSignature
            {
                Name = parsedCommit.committer.name,
                Email = parsedCommit.committer.email,
                Date = DateTimeOffset.FromUnixTimeSeconds(parsedCommit.committer.date)
            };

            List<GitAdditionalHeader>? additionalHeaders = null;

            if (parsedCommit.additionalHeaders.Length != 0)
            {
                additionalHeaders = new List<GitAdditionalHeader>();

                foreach (var h in parsedCommit.additionalHeaders)
                {
                    additionalHeaders.Add(new GitAdditionalHeader
                    {
                        Key = h.name,
                        Value = h.value
                    });
                }
            }

            string message = parsedCommit.message;

            return new GitCommit
            {
                Tree = tree,
                Sha = sha,
                FirstParent = firstParent,
                SecondParent = secondParent,
                AdditionalParents = additionalParents,
                Author = author,
                Committer = committer,
                AdditionalHeaders = additionalHeaders,
                Message = message
            };
        }
    }
}
