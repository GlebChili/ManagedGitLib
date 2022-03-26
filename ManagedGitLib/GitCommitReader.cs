﻿#nullable enable

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

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
            var buffer = commit;

            var tree = ReadTree(buffer.Slice(0, TreeLineLength));

            buffer = buffer.Slice(TreeLineLength);

            GitObjectId? firstParent = null, secondParent = null;
            List<GitObjectId>? additionalParents = null;
            List<GitObjectId> parents = new List<GitObjectId>();
            while (TryReadParent(buffer, out GitObjectId parent))
            {
                if (!firstParent.HasValue)
                {
                    firstParent = parent;
                }
                else if (!secondParent.HasValue)
                {
                    secondParent = parent;
                }
                else
                {
                    additionalParents ??= new List<GitObjectId>();
                    additionalParents.Add(parent);
                }

                buffer = buffer.Slice(ParentLineLength);
            }

            GitSignature authorSignature = default;

            if (!TryReadAuthor(buffer, out authorSignature, out int authorLineLength))
            {
                throw new GitException("Unable to read commit author");
            }

            buffer = buffer.Slice(authorLineLength);

            GitSignature commiterSignature = default;

            if (!TryReadCommitter(buffer, out commiterSignature, out int committerLineLength))
            {
                throw new GitException("Unable to read commit committer");
            }

            buffer = buffer.Slice(committerLineLength);

            while (TryReadAdditionalHeaders(buffer, out int additionalHeaderLength))
            {
                buffer = buffer.Slice(additionalHeaderLength);
            }

            string message = "";

            if (!TryReadMessage(buffer, out message))
            {
                throw new GitException("Unable to read commit message");
            }

            return new GitCommit()
            {
                Sha = sha,
                FirstParent = firstParent,
                SecondParent = secondParent,
                AdditionalParents = additionalParents,
                Tree = tree,
                Author = authorSignature,
                Committer = commiterSignature,
                Message = message
            };
        }

        private static GitObjectId ReadTree(ReadOnlySpan<byte> line)
        {
            // Format: tree d8329fc1cc938780ffdd9f94e0d364e0ea74f579\n
            // 47 bytes: 
            //  tree: 5 bytes
            //  space: 1 byte
            //  hash: 40 bytes
            //  \n: 1 byte
            bool hasCorrectPrefix = line.Slice(0, TreeStart.Length).SequenceEqual(TreeStart);
            bool hasCorrectLength = line[TreeLineLength - 1] == (byte)'\n';

            if (!(hasCorrectLength && hasCorrectPrefix))
            {
                throw new Exception("Unable to read commit tree");
            }

            return GitObjectId.ParseHex(line.Slice(TreeStart.Length, 40));
        }

        private static bool TryReadParent(ReadOnlySpan<byte> line, out GitObjectId parent)
        {
            // Format: "parent ef079ebcca375f6fd54aa0cb9f35e3ecc2bb66e7\n"
            parent = GitObjectId.Empty;

            if (!line.Slice(0, ParentStart.Length).SequenceEqual(ParentStart))
            {
                return false;
            }

            if (line[ParentLineLength - 1] != (byte)'\n')
            {
                return false;
            }

            parent = GitObjectId.ParseHex(line.Slice(ParentStart.Length, 40));
            return true;
        }

        private static bool TryReadAuthor(ReadOnlySpan<byte> line, out GitSignature signature, out int lineLength)
        {
            signature = default;
            lineLength = 0;

            if (!line.Slice(0, AuthorStart.Length).SequenceEqual(AuthorStart))
            {
                return false;
            }

            line = line.Slice(AuthorStart.Length);

            int emailStart = line.IndexOf((byte)'<');
            int emailEnd = line.IndexOf((byte)'>');
            int lineEnd = line.IndexOf((byte)'\n');

            lineLength = AuthorStart.Length + lineEnd + 1;

            var name = line.Slice(0, emailStart - 1);
            var email = line.Slice(emailStart + 1, emailEnd - emailStart - 1);
            var time = line.Slice(emailEnd + 2, lineEnd - emailEnd - 2);

            if (name.Length != 0)
            {
                signature.Name = GitRepository.GetString(name);
            }
            else
            {
                signature.Name = "";
            }

            if (email.Length != 0)
            {
                signature.Email = GitRepository.GetString(email);
            }
            else
            {
                signature.Email = "";
            }

            var offsetStart = time.IndexOf((byte)' ');
            var ticks = long.Parse(GitRepository.GetString(time.Slice(0, offsetStart)));
            signature.Date = DateTimeOffset.FromUnixTimeSeconds(ticks);

            return true;
        }

        private static bool TryReadCommitter(ReadOnlySpan<byte> line, out GitSignature signature, out int lineLength)
        {
            signature = default;
            lineLength = 0;

            if (!line.Slice(0, CommitterStart.Length).SequenceEqual(CommitterStart))
            {
                return false;
            }

            line = line.Slice(CommitterStart.Length);

            int emailStart = line.IndexOf((byte)'<');
            int emailEnd = line.IndexOf((byte)'>');
            var lineEnd = line.IndexOf((byte)'\n');

            lineLength = CommitterStart.Length + lineEnd + 1;

            var name = line.Slice(0, emailStart - 1);
            var email = line.Slice(emailStart + 1, emailEnd - emailStart - 1);
            var time = line.Slice(emailEnd + 2, lineEnd - emailEnd - 2);

            if (name.Length != 0)
            {
                signature.Name = GitRepository.GetString(name);
            }
            else
            {
                signature.Name = "";
            }

            if (email.Length != 0)
            {
                signature.Email = GitRepository.GetString(email);
            }
            else
            {
                signature.Email = "";
            }

            var offsetStart = time.IndexOf((byte)' ');
            var ticks = long.Parse(GitRepository.GetString(time.Slice(0, offsetStart)));
            signature.Date = DateTimeOffset.FromUnixTimeSeconds(ticks);

            return true;
        }

        private static bool TryReadAdditionalHeaders(ReadOnlySpan<byte> buffer, out int additionalHeaderLength)
        {
            additionalHeaderLength = 0;

            if (buffer.Slice(0, MessageStart.Length).SequenceEqual(MessageStart))
            {
                return false;
            }

            var lineEnd = buffer.IndexOf((byte)'\n');

            additionalHeaderLength = lineEnd + 1;

            return true;
        }

        private static bool TryReadMessage(ReadOnlySpan<byte> buffer, out string message)
        {
            message = "";

            if (!buffer.Slice(0, MessageStart.Length).SequenceEqual(MessageStart))
            {
                return false;
            }

            buffer = buffer.Slice(MessageStart.Length);

            message = GitRepository.Encoding.GetString(buffer.ToArray());

            return true;
        }
    }
}
