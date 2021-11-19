using System;
using System.Buffers;
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

        [Fact]
        public void ReadCommitWithSignature()
        {
            using (Stream stream = TestUtilities.GetEmbeddedResource(@"commit-7507fb2859c12f6c561efc23d48dd1be0fc6cdee"))
            {
                var commit = GitCommitReader.Read(stream, GitObjectId.Parse("7507fb2859c12f6c561efc23d48dd1be0fc6cdee"));

                Assert.Equal("7507fb2859c12f6c561efc23d48dd1be0fc6cdee", commit.Sha.ToString());
                Assert.Equal("51a2be3228fb6c8bed8a3141384a25b36928136c", commit.Tree.ToString());

                Assert.Equal("e368cf51d8233bee7b0fcbc5174fe7030c882407", commit.FirstParent.Value.ToString());

                var author = commit.Author;

                Assert.Equal("monojenkins", author.Name);
                Assert.Equal("jo.shields+jenkins@xamarin.com", author.Email);
                Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1599746832), author.Date);

                var committer = commit.Committer;

                Assert.Equal("GitHub", committer.Name);
                Assert.Equal("noreply@github.com", committer.Email);
                Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1599746832), committer.Date);

                byte[] sigbytes = GitRepository.Encoding.GetBytes(@"-----BEGIN PGP SIGNATURE-----

 wsBcBAABCAAQBQJfWjMQCRBK7hj4Ov3rIwAAdHIIABsTXgfvq1GoksuPtrQ5z3H4
 rL7zsWMfz0+Cb4VUaN5hCoHx58RYXMdmf/VLvFsQacUOvCVevAKaFm1g6fckJ0Rg
 p7SkE6Np9v0OisAj8SrHqsHNk9aoTvu2781doKtQmsBWXB+NYxNR3v3jmehn6h1v
 lTSwn4NHZRhDLEo1BRQR/ZuqZin437/73M6BY2LHwoEyA1ZDFigHHyuwaS4jWzn3
 qESGl9zNddpUvqkJtjDzQp7eoPI/fr76fBuFyrUMVe0yziNbuBUAU6UJKO0eS5y9
 QDN2Jfh1WnagHZ7L6GgYn72CK6q3QYFvNQSDHGJroj3Lc6rmxeD0/Jk1X43fDTE=
 =SzYu
 -----END PGP SIGNATURE-----
");

                Assert.Equal<byte>(sigbytes, commit.GpgSignature);

                using Stream commitMessageStream = TestUtilities.GetEmbeddedResource(@"commit_message.txt");
                byte[] commitMessageBuffer = new byte[commitMessageStream.Length];
                commitMessageStream.ReadAll(commitMessageBuffer);
                string expectedCommitMessage = GitRepository.Encoding.GetString(commitMessageBuffer);

                Assert.Equal(expectedCommitMessage, commit.Message);
            }
        }
    }
}
