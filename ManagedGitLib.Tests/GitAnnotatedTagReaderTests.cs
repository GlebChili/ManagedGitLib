using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace ManagedGitLib.Tests
{
    public class GitAnnotatedTagReaderTests
    {
        [Fact]
        public void ReadAnnotatedTag()
        {
            using Stream stream = TestUtilities.GetEmbeddedResource(@"tag-ceaa6f5b53565910b9b2dd211a9d8284445a9a9c");

            GitTag tag =
                GitAnnotatedTagReader.Read(stream, GitObjectId.Parse("ceaa6f5b53565910b9b2dd211a9d8284445a9a9c"));

            Assert.Equal(GitObjectId.Parse("ceaa6f5b53565910b9b2dd211a9d8284445a9a9c"), tag.Sha);
            
            Assert.Equal(GitObjectId.Parse("bef1e6335812d32f8eab648c0228fc624b9f8357"), tag.Target);

            Assert.True(tag.IsAnnotated);

            Assert.Equal("commit", tag.TargetType);

            Assert.Equal("mono-6.6.0.161", tag.Name);

            GitSignature? tagger = tag.Tagger;

            Assert.NotNull(tagger);
            
            Assert.Equal("Xamarin Public Jenkins (auto-signing)", tagger!.Value.Name);
            Assert.Equal("releng@xamarin.com", tagger!.Value.Email);
            Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1575995726), tagger!.Value.Date);

            Assert.Equal("Tag mono-6.6.0.161 for stable branch\n", tag.Message);
        }
    }
}
