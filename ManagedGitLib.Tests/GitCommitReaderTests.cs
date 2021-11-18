using System;
using System.IO;
using Xunit;

namespace ManagedGitLib.Tests
{
    public class GitCommitReaderTests
    {
        [Fact]
        public void ReadNoGpgSignatureTest()
        {
            using (Stream stream = TestUtilities.GetEmbeddedResource(@"commit-d56dc3ed179053abef2097d1120b4507769bcf1a"))
            {
                var commit = GitCommitReader.Read(stream, GitObjectId.Parse("d56dc3ed179053abef2097d1120b4507769bcf1a"));

                Assert.Equal("d56dc3ed179053abef2097d1120b4507769bcf1a", commit.Sha.ToString());
                Assert.Equal("f914b48023c7c804a4f3be780d451f31aef74ac1", commit.Tree.ToString());

                Assert.Collection(
                    commit.Parents,
                    c => Assert.Equal("4497b0eaaa89abf0e6d70961ad5f04fd3a49cbc6", c.ToString()),
                    c => Assert.Equal("0989e8fe0cd0e0900173b26decdfb24bc0cc8232", c.ToString()));

                var author = commit.Author;

                Assert.Equal("Andrew Arnott", author.Name);
                Assert.Equal(new DateTimeOffset(2020, 10, 6, 13, 40, 09, TimeSpan.FromHours(-6)), author.Date);
                Assert.Equal("andrewarnott@gmail.com", author.Email);

                var committer = commit.Committer;

                Assert.Equal("Andrew Arnott", committer.Name);
                Assert.Equal(new DateTimeOffset(2020, 10, 6, 13, 40, 09, TimeSpan.FromHours(-6)), author.Date);
                Assert.Equal("andrewarnott@gmail.com", committer.Email);

                Assert.Equal("Merge branch 'v3.3'", commit.Message);

                Assert.Null(commit.GpgSignature);
            }
        }
    }
}
