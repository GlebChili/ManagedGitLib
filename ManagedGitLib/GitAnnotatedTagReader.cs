#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Buffers;

namespace ManagedGitLib
{
    /// <summary>
    /// Reads an annotated <see cref="GitTag"/> object.
    /// </summary>
    public static class GitAnnotatedTagReader
    {
        static readonly byte[] ObjectStart = GitRepository.Encoding.GetBytes("object ");
        static readonly byte[] TypeStart = GitRepository.Encoding.GetBytes("type ");
        static readonly byte[] TagStart = GitRepository.Encoding.GetBytes("tag ");
        static readonly byte[] TaggerStart = GitRepository.Encoding.GetBytes("tagger ");
        static readonly byte[] MessageStart = GitRepository.Encoding.GetBytes("\n");

        static readonly int ObjectLineLength = 48;

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
            var buffer = tag;

            GitObjectId targetObject = ReadObject(buffer);

            buffer = buffer.Slice(ObjectLineLength);

            string type = ReadType(buffer, out int typeLineLength);

            buffer = buffer.Slice(typeLineLength);

            string tagName = ReadName(buffer, out int nameLineLength);

            buffer = buffer.Slice(nameLineLength);

            GitSignature tagger = ReadTagger(buffer, out int taggerLineLength);

            buffer = buffer.Slice(taggerLineLength);

            while (TryReadAdditionalHeaders(buffer, out int additionalHeaderLength))
            {
                buffer = buffer.Slice(additionalHeaderLength);
            }

            string message = ReadMessage(buffer);

            return new GitTag
            {
                Sha = sha,
                Target = targetObject,
                TargetType = type,
                Name = tagName,
                Tagger = tagger,
                Message = message
            };
        }

        static GitObjectId ReadObject(ReadOnlySpan<byte> buffer)
        {
            if (!buffer.Slice(0, ObjectStart.Length).SequenceEqual(ObjectStart) || buffer[ObjectLineLength - 1] != (byte)'\n')
            {
                throw new GitException("Unable to read target object of the annotated tag");
            }

            return GitObjectId.ParseHex(buffer.Slice(ObjectStart.Length, 40));
        }

        static string ReadType(ReadOnlySpan<byte> buffer, out int typeLineLength)
        {
            typeLineLength = 0;

            if (!buffer.Slice(0, TypeStart.Length).SequenceEqual(TypeStart))
            {
                throw new GitException("Unable to read type of the annotated tag");
            }

            buffer = buffer.Slice(TypeStart.Length);

            int lineEnd = buffer.IndexOf((byte)'\n');

            typeLineLength = TypeStart.Length + lineEnd + 1;

            return GitRepository.Encoding.GetString(buffer.Slice(0, lineEnd).ToArray());
        }

        static string ReadName(ReadOnlySpan<byte> buffer, out int nameLineLength)
        {
            nameLineLength = 0;

            if (!buffer.Slice(0, TagStart.Length).SequenceEqual(TagStart))
            {
                throw new GitException("Unable to read name of the annotated tag");
            }

            buffer = buffer.Slice(TagStart.Length);

            int lineEnd = buffer.IndexOf((byte)'\n');

            nameLineLength = TagStart.Length + lineEnd + 1;

            return GitRepository.Encoding.GetString(buffer.Slice(0, lineEnd).ToArray());
        }

        static GitSignature ReadTagger(ReadOnlySpan<byte> buffer, out int taggerLineLength)
        {
            taggerLineLength = 0;

            if (!buffer.Slice(0, TaggerStart.Length).SequenceEqual(TaggerStart))
            {
                throw new GitException("Unable to read tagger of the annotated tag");
            }

            var line = buffer.Slice(TaggerStart.Length);

            int emailStart = line.IndexOf((byte)'<');
            int emailEnd = line.IndexOf((byte)'>');
            int lineEnd = line.IndexOf((byte)'\n');

            taggerLineLength = TaggerStart.Length + lineEnd + 1;

            var name = line.Slice(0, emailStart - 1);
            var email = line.Slice(emailStart + 1, emailEnd - emailStart - 1);
            var time = line.Slice(emailEnd + 2, lineEnd - emailEnd - 2);

            GitSignature signature = default;

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

            return signature;
        }

        static bool TryReadAdditionalHeaders(ReadOnlySpan<byte> buffer, out int additionalHeaderLength)
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

        static string ReadMessage(ReadOnlySpan<byte> buffer)
        {
            if (!buffer.Slice(0, MessageStart.Length).SequenceEqual(MessageStart))
            {
                throw new GitException("Unable to read message of the annotated commit");
            }

            buffer = buffer.Slice(MessageStart.Length);

            return GitRepository.Encoding.GetString(buffer.ToArray()).TrimEnd('\n');
        }
    }
}
